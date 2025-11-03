"""
Face Recognition Service
Core recognition logic with rate limiting and session management
"""

import time
import uuid
import json
from pathlib import Path
from typing import Dict, List, Optional, Tuple
from datetime import datetime
import logging

from config.settings import settings, get_session_path
from services.image_processor import ImageProcessor
from services.embedding_cache import EmbeddingCache

logger = logging.getLogger(__name__)


class FaceRecognitionService:
    """
    Main service for face recognition operations
    Handles registration and recognition with rate limiting
    """
    
    def __init__(self):
        """Initialize face recognition service"""
        self.image_processor = ImageProcessor()
        self.embedding_cache = EmbeddingCache(preload=settings.CACHE_PRELOAD)
        self.fps_limit = settings.RECOGNITION_FPS_LIMIT
        self.last_recognition_time = 0
        
        logger.info(f"FaceRecognitionService initialized (FPS limit: {self.fps_limit})")
    
    def _apply_rate_limit(self):
        """Apply rate limiting for recognition operations"""
        if self.fps_limit > 0:
            min_interval = 1.0 / self.fps_limit
            current_time = time.time()
            time_since_last = current_time - self.last_recognition_time
            
            if time_since_last < min_interval:
                sleep_time = min_interval - time_since_last
                logger.debug(f"Rate limiting: sleeping {sleep_time:.3f}s")
                time.sleep(sleep_time)
            
            self.last_recognition_time = time.time()
    
    def register_student(
        self,
        image_path: str,
        student_id: str,
        preprocess: bool = True
    ) -> Dict:
        """
        Register a student's face
        
        Args:
            image_path: Path to student's face image
            student_id: Unique student identifier
            preprocess: Whether to preprocess the image
            
        Returns:
            Registration result dictionary
        """
        try:
            start_time = time.time()
            logger.info(f"Registering student: {student_id}")
            
            # Preprocess image if requested
            if preprocess:
                image_path = self.image_processor.preprocess_image(image_path)
            
            # Validate single face
            is_valid, message, face_data = self.image_processor.validate_single_face(image_path)
            if not is_valid:
                return {
                    'success': False,
                    'error': message,
                    'student_id': student_id
                }
            
            # Generate embedding
            embedding = self.embedding_cache.generate_embedding(image_path)
            if embedding is None:
                return {
                    'success': False,
                    'error': 'Failed to generate face embedding',
                    'student_id': student_id
                }
            
            # Cache embedding
            metadata = {
                'registered_at': datetime.utcnow().isoformat(),
                'image_path': image_path,
                'confidence': face_data['confidence']
            }
            
            success = self.embedding_cache.set_embedding(
                student_id=student_id,
                embedding=embedding,
                metadata=metadata,
                save_to_disk=True
            )
            
            if not success:
                return {
                    'success': False,
                    'error': 'Failed to cache embedding',
                    'student_id': student_id
                }
            
            # Calculate processing time
            processing_time = time.time() - start_time
            
            logger.info(f"✅ Student {student_id} registered successfully ({processing_time:.2f}s)")
            
            return {
                'success': True,
                'message': 'Student registered successfully',
                'student_id': student_id,
                'face_confidence': face_data['confidence'],
                'embedding_shape': embedding.shape,
                'processing_time': round(processing_time, 3)
            }
        
        except Exception as e:
            logger.error(f"Error registering student {student_id}: {e}")
            return {
                'success': False,
                'error': str(e),
                'student_id': student_id
            }
    
    def recognize_faces(
        self,
        image_path: str,
        session_id: Optional[str] = None,
        preprocess: bool = True,
        save_results: bool = True
    ) -> Dict:
        """
        Recognize faces in an image
        
        Args:
            image_path: Path to image with faces
            session_id: Optional session identifier for tracking
            preprocess: Whether to preprocess the image
            save_results: Whether to save results to session folder
            
        Returns:
            Recognition results dictionary
        """
        try:
            start_time = time.time()
            
            # Apply rate limiting
            self._apply_rate_limit()
            
            # Generate session ID if not provided
            if session_id is None:
                session_id = str(uuid.uuid4())
            
            logger.info(f"Recognizing faces (session: {session_id})")
            
            # Preprocess image if requested
            if preprocess:
                image_path = self.image_processor.preprocess_image(image_path)
            
            # Extract all faces
            faces = self.image_processor.extract_faces_from_frame(
                image_path,
                min_confidence=0.5
            )
            
            if not faces:
                return {
                    'success': False,
                    'message': 'No faces detected in the image',
                    'session_id': session_id,
                    'recognized_students': [],
                    'total_faces_detected': 0
                }
            
            logger.info(f"Detected {len(faces)} face(s)")
            
            # Generate embeddings for each face
            face_embeddings = []
            for face in faces:
                # Save face region temporarily
                face_path = str(settings.TEMP_FOLDER / f"face_{uuid.uuid4()}.jpg")
                self.image_processor.crop_face_region(
                    image_path=image_path,
                    face_region=face['region'],
                    output_path=face_path
                )
                
                # Generate embedding
                embedding = self.embedding_cache.generate_embedding(face_path)
                face_embeddings.append({
                    'face': face,
                    'embedding': embedding,
                    'face_path': face_path
                })
            
            # Match against cached embeddings
            recognized_students = []
            for face_data in face_embeddings:
                if face_data['embedding'] is not None:
                    student_id, distance, confidence = self.embedding_cache.find_best_match(
                        query_embedding=face_data['embedding'],
                        threshold=settings.CONFIDENCE_THRESHOLD
                    )
                    
                    if student_id:
                        recognized_students.append({
                            'student_id': student_id,
                            'confidence': round(confidence, 4),
                            'distance': round(distance, 4),
                            'face_region': face_data['face']['region'],
                            'detection_confidence': face_data['face']['confidence']
                        })
            
            # Calculate processing time
            processing_time = time.time() - start_time
            
            # Prepare results
            results = {
                'success': True,
                'message': f"Recognized {len(recognized_students)} student(s)",
                'session_id': session_id,
                'recognized_students': recognized_students,
                'total_faces_detected': len(faces),
                'total_recognized': len(recognized_students),
                'processing_time': round(processing_time, 3),
                'timestamp': datetime.utcnow().isoformat()
            }
            
            # Save results to session folder
            if save_results:
                self._save_session_results(session_id, results)
            
            logger.info(f"✅ Recognition complete: {len(recognized_students)}/{len(faces)} faces recognized ({processing_time:.2f}s)")
            
            return results
        
        except Exception as e:
            logger.error(f"Error in recognize_faces: {e}")
            return {
                'success': False,
                'error': str(e),
                'session_id': session_id,
                'recognized_students': []
            }
    
    def _save_session_results(self, session_id: str, results: Dict):
        """
        Save recognition results to session folder
        
        Args:
            session_id: Session identifier
            results: Recognition results dictionary
        """
        try:
            # Get session path
            session_path = get_session_path(session_id)
            
            # Create subdirectories
            (session_path / "detected_faces").mkdir(exist_ok=True)
            (session_path / "embeddings").mkdir(exist_ok=True)
            
            # Save results JSON
            results_file = session_path / "results.json"
            
            # Load existing results if file exists
            existing_results = []
            if results_file.exists():
                with open(results_file, 'r') as f:
                    existing_results = json.load(f)
            
            # Append new results
            existing_results.append(results)
            
            # Save updated results
            with open(results_file, 'w') as f:
                json.dump(existing_results, f, indent=2)
            
            logger.debug(f"Session results saved: {results_file}")
        
        except Exception as e:
            logger.error(f"Error saving session results: {e}")
    
    def get_session_results(self, session_id: str) -> Optional[List[Dict]]:
        """
        Get all results for a session
        
        Args:
            session_id: Session identifier
            
        Returns:
            List of result dictionaries or None if not found
        """
        try:
            session_path = get_session_path(session_id)
            results_file = session_path / "results.json"
            
            if not results_file.exists():
                return None
            
            with open(results_file, 'r') as f:
                return json.load(f)
        
        except Exception as e:
            logger.error(f"Error getting session results: {e}")
            return None
    
    def get_cache_statistics(self) -> Dict:
        """
        Get cache statistics
        
        Returns:
            Dictionary with cache statistics
        """
        cache_stats = self.embedding_cache.get_cache_stats()
        cache_stats.update({
            'fps_limit': self.fps_limit,
            'model': settings.DEEPFACE_MODEL,
            'detector': settings.DEEPFACE_DETECTOR,
            'threshold': settings.CONFIDENCE_THRESHOLD
        })
        return cache_stats
    
    def clear_cache(self, student_id: Optional[str] = None):
        """
        Clear embedding cache
        
        Args:
            student_id: Student to remove (None = clear all)
        """
        self.embedding_cache.clear_cache(student_id)
        logger.info(f"Cache cleared" + (f" for student {student_id}" if student_id else ""))
