"""
Face Recognition API Service - Refactored
Clean, modular Flask application with proper separation of concerns
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
from datetime import datetime
import logging
import uuid

from config import settings, setup_logging
from services import ImageProcessor, EmbeddingCache, FaceRecognitionService, SupabaseService
from models.schemas import (
    RegisterStudentResponse,
    RecognizeFacesResponse,
    DetectFacesResponse,
    HealthResponse,
    CacheStats,
    DetectedFace,
    RecognizedStudent,
    FaceRegion
)
from utils import FileHandler, validate_image_file, validate_student_id, validate_class_id, get_now

# Setup logging
logger = setup_logging(log_level="INFO" if not settings.FLASK_DEBUG else "DEBUG")

# Initialize Flask app
app = Flask(__name__)
CORS(app)  # Enable CORS for all routes

# Initialize services
face_service = FaceRecognitionService()
supabase_service = SupabaseService()
file_handler = FileHandler()

logger.info("="*60)
logger.info("🚀 Face Recognition API Service Starting...")
logger.info(f"📦 Model: {settings.DEEPFACE_MODEL}")
logger.info(f"📏 Distance Metric: {settings.DEEPFACE_DISTANCE_METRIC}")
logger.info(f"🔍 Detector: {settings.DEEPFACE_DETECTOR}")
logger.info(f"🎯 Confidence Threshold: {settings.CONFIDENCE_THRESHOLD}")
logger.info(f"⚡ FPS Limit: {settings.RECOGNITION_FPS_LIMIT}")
logger.info(f"💾 Cache Preload: {settings.CACHE_PRELOAD}")
logger.info("="*60)


# ============================================================================
# ENDPOINT 1: Health Check
# ============================================================================

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint with cache statistics"""
    try:
        cache_stats = face_service.get_cache_statistics()
        
        response = HealthResponse(
            status="healthy",
            service="Face Recognition API",
            model=settings.DEEPFACE_MODEL,
            cache_stats=CacheStats(**cache_stats),
            timestamp=get_now().isoformat()
        )
        
        return jsonify(response.dict()), 200
    
    except Exception as e:
        logger.error(f"Health check error: {e}")
        return jsonify({
            "status": "error",
            "error": str(e),
            "timestamp": get_now().isoformat()
        }), 500


# ============================================================================
# ENDPOINT 2: Detect Faces
# ============================================================================

@app.route('/api/face/detect', methods=['POST'])
def detect_faces():
    """Detect faces in uploaded image"""
    temp_file = None
    try:
        # Validate request
        if 'image' not in request.files:
            return jsonify(DetectFacesResponse(
                success=False,
                error="No image file provided",
                detected_faces=[],
                total_faces=0
            ).dict()), 400
        
        # Validate file
        file = request.files['image']
        is_valid, error_msg = validate_image_file(file)
        if not is_valid:
            return jsonify(DetectFacesResponse(
                success=False,
                error=error_msg,
                detected_faces=[],
                total_faces=0
            ).dict()), 400
        
        # Save uploaded file
        temp_file, error = file_handler.save_uploaded_file(file)
        if error:
            return jsonify(DetectFacesResponse(
                success=False,
                error=error,
                detected_faces=[],
                total_faces=0
            ).dict()), 400
        
        # Extract faces
        faces = face_service.image_processor.extract_faces_from_frame(temp_file, min_confidence=0.5)
        
        if not faces:
            return jsonify(DetectFacesResponse(
                success=False,
                message="No faces detected in the image",
                detected_faces=[],
                total_faces=0
            ).dict()), 200
        
        # Format response
        detected_faces = [
            DetectedFace(
                face_id=str(uuid.uuid4()),
                confidence=face['confidence'],
                region=FaceRegion(**face['region'])
            )
            for face in faces
        ]
        
        response = DetectFacesResponse(
            success=True,
            message=f"Detected {len(detected_faces)} face(s)",
            detected_faces=detected_faces,
            total_faces=len(detected_faces)
        )
        
        return jsonify(response.dict()), 200
    
    except Exception as e:
        logger.error(f"Error in detect_faces: {e}")
        return jsonify(DetectFacesResponse(
            success=False,
            error=str(e),
            detected_faces=[],
            total_faces=0
        ).dict()), 500
    
    finally:
        file_handler.cleanup_file(temp_file)


# ============================================================================
# ENDPOINT 3: Register Student
# ============================================================================

@app.route('/api/face/register', methods=['POST'])
def register_student():
    """Register a student's face"""
    temp_file = None
    try:
        # Validate image
        if 'image' not in request.files:
            return jsonify(RegisterStudentResponse(
                success=False,
                error="No image file provided",
                student_id=""
            ).dict()), 400
        
        # Validate student ID
        student_id = request.form.get('studentId')
        is_valid, error_msg = validate_student_id(student_id)
        if not is_valid:
            return jsonify(RegisterStudentResponse(
                success=False,
                error=error_msg,
                student_id=student_id or ""
            ).dict()), 400
        
        # Validate file
        file = request.files['image']
        is_valid, error_msg = validate_image_file(file)
        if not is_valid:
            return jsonify(RegisterStudentResponse(
                success=False,
                error=error_msg,
                student_id=student_id
            ).dict()), 400
        
        # Save uploaded file
        temp_file, error = file_handler.save_uploaded_file(file)
        if error:
            return jsonify(RegisterStudentResponse(
                success=False,
                error=error,
                student_id=student_id
            ).dict()), 400
        
        # Register student
        result = face_service.register_student(
            image_path=temp_file,
            student_id=student_id,
            preprocess=True
        )
        
        if not result['success']:
            return jsonify(RegisterStudentResponse(
                success=False,
                error=result.get('error', 'Registration failed'),
                student_id=student_id
            ).dict()), 400
        
        # Upload to Supabase if enabled
        face_url = None
        if supabase_service.is_enabled():
            try:
                from datetime import datetime
                filename = f"{student_id}_{get_now().strftime('%Y%m%d_%H%M%S')}.jpg"
                face_url, _ = supabase_service.save_student_face(
                    local_path=temp_file,
                    student_id=student_id,
                    filename=filename
                )
                logger.info(f"✅ Uploaded to Supabase: {face_url}")
            except Exception as e:
                logger.warning(f"⚠️ Supabase upload failed: {e}")
        
        # Build response
        response = RegisterStudentResponse(
            success=True,
            message="Student registered successfully",
            student_id=student_id,
            face_confidence=result.get('face_confidence'),
            embedding_shape=result.get('embedding_shape'),
            processing_time=result.get('processing_time')
        )
        
        return jsonify(response.dict()), 200
    
    except Exception as e:
        logger.error(f"Error in register_student: {e}")
        return jsonify(RegisterStudentResponse(
            success=False,
            error=str(e),
            student_id=request.form.get('studentId', '')
        ).dict()), 500
    
    finally:
        file_handler.cleanup_file(temp_file)


# ============================================================================
# ENDPOINT 4: Recognize Faces
# ============================================================================

@app.route('/api/face/recognize', methods=['POST'])
def recognize_faces():
    """Recognize faces in uploaded image"""
    temp_file = None
    try:
        # Validate image
        if 'image' not in request.files:
            return jsonify(RecognizeFacesResponse(
                success=False,
                message="No image file provided",
                error="No image file provided",
                session_id=str(uuid.uuid4()),
                recognized_students=[],
                total_faces_detected=0
            ).dict()), 400
        
        # Get class ID (optional)
        class_id = request.form.get('classId')
        
        # Validate file
        file = request.files['image']
        is_valid, error_msg = validate_image_file(file)
        if not is_valid:
            return jsonify(RecognizeFacesResponse(
                success=False,
                message=error_msg,
                error=error_msg,
                session_id=str(uuid.uuid4()),
                recognized_students=[],
                total_faces_detected=0
            ).dict()), 400
        
        # Save uploaded file
        temp_file, error = file_handler.save_uploaded_file(file)
        if error:
            return jsonify(RecognizeFacesResponse(
                success=False,
                message=error,
                error=error,
                session_id=str(uuid.uuid4()),
                recognized_students=[],
                total_faces_detected=0
            ).dict()), 400
        
        # Note: Database sync removed from here to avoid latency on first capture
        # Database should be synced at startup or via manual sync endpoint
        
        # Recognize faces
        result = face_service.recognize_faces(
            image_path=temp_file,
            session_id=None,
            preprocess=True,
            save_results=True
        )
        
        if not result['success']:
            return jsonify(RecognizeFacesResponse(
                success=False,
                message=result.get('error', 'Recognition failed'),
                error=result.get('error'),
                session_id=result.get('session_id', str(uuid.uuid4())),
                recognized_students=[],
                total_faces_detected=0
            ).dict()), 500
        
        # Build response
        recognized_students = [
            RecognizedStudent(
                student_id=student['student_id'],
                confidence=student['confidence'],
                distance=student['distance'],
                face_region=FaceRegion(**student['face_region']),
                detection_confidence=student.get('detection_confidence')
            )
            for student in result.get('recognized_students', [])
        ]
        
        response = RecognizeFacesResponse(
            success=True,
            message=result.get('message', 'Recognition complete'),
            session_id=result['session_id'],
            recognized_students=recognized_students,
            total_faces_detected=result['total_faces_detected'],
            total_recognized=result.get('total_recognized', len(recognized_students)),
            processing_time=result.get('processing_time'),
            timestamp=result.get('timestamp')
        )
        
        return jsonify(response.dict()), 200
    
    except Exception as e:
        logger.error(f"Error in recognize_faces: {e}")
        return jsonify(RecognizeFacesResponse(
            success=False,
            message=str(e),
            error=str(e),
            session_id=str(uuid.uuid4()),
            recognized_students=[],
            total_faces_detected=0
        ).dict()), 500
    
    finally:
        file_handler.cleanup_file(temp_file)


# ============================================================================
# ENDPOINT 5: Get Session Results
# ============================================================================

@app.route('/api/session/<session_id>/results', methods=['GET'])
def get_session_results(session_id):
    """Get results for a specific session"""
    try:
        results = face_service.get_session_results(session_id)
        
        if results is None:
            return jsonify({
                "success": False,
                "error": "Session not found"
            }), 404
        
        return jsonify({
            "success": True,
            "session_id": session_id,
            "results": results
        }), 200
    
    except Exception as e:
        logger.error(f"Error getting session results: {e}")
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


# ============================================================================
# ENDPOINT 6: Cache Statistics
# ============================================================================

@app.route('/api/cache/stats', methods=['GET'])
def get_cache_stats():
    """Get cache statistics"""
    try:
        stats = face_service.get_cache_statistics()
        return jsonify({
            "success": True,
            "cache_stats": stats
        }), 200
    
    except Exception as e:
        logger.error(f"Error getting cache stats: {e}")
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


# ============================================================================
# ENDPOINT 7: Sync Database from Supabase
# ============================================================================

@app.route('/api/database/sync', methods=['POST'])
def sync_database():
    """Manually sync class database from Supabase"""
    try:
        # Get class ID from request
        class_id = request.json.get('classId') if request.json else None
        
        if not class_id:
            return jsonify({
                "success": False,
                "error": "classId is required"
            }), 400
        
        if not supabase_service.is_enabled():
            return jsonify({
                "success": False,
                "error": "Supabase is not enabled"
            }), 400
        
        # Perform sync
        from config.settings import get_class_database_path
        class_db_path = get_class_database_path(class_id)
        
        logger.info(f"📥 Manual sync requested for class {class_id}")
        student_count, message = supabase_service.sync_class_students(class_id, class_db_path)
        
        # Reload embeddings cache
        face_service.embedding_cache._load_all_embeddings()
        
        return jsonify({
            "success": True,
            "message": message,
            "student_count": student_count,
            "class_id": class_id
        }), 200
    
    except Exception as e:
        logger.error(f"Error syncing database: {e}")
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


# ============================================================================
# ENDPOINT 8: Cleanup Class Database
# ============================================================================

@app.route('/api/database/cleanup', methods=['POST'])
def cleanup_database():
    """Delete class database from local storage"""
    try:
        # Get class ID from request
        class_id = request.json.get('classId') if request.json else None
        
        if not class_id:
            return jsonify({
                "success": False,
                "error": "classId is required"
            }), 400
        
        # Delete class database folder
        from config.settings import get_class_database_path
        import shutil
        
        class_db_path = get_class_database_path(class_id)
        
        if class_db_path.exists():
            shutil.rmtree(class_db_path)
            logger.info(f"🗑️  Deleted class database: {class_db_path}")
            
            # Clear embeddings cache for this class
            face_service.embedding_cache.clear_cache()
            
            return jsonify({
                "success": True,
                "message": f"Cleaned up database for class {class_id}",
                "deleted_path": str(class_db_path)
            }), 200
        else:
            return jsonify({
                "success": True,
                "message": f"No database found for class {class_id}",
                "deleted_path": None
            }), 200
    
    except Exception as e:
        logger.error(f"Error cleaning up database: {e}")
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


# ============================================================================
# ENDPOINT 9: Clear Cache
# ============================================================================

@app.route('/api/cache/clear', methods=['POST'])
def clear_cache():
    """Clear embedding cache"""
    try:
        student_id = request.json.get('studentId') if request.json else None
        face_service.clear_cache(student_id)
        
        message = f"Cache cleared for student {student_id}" if student_id else "Cache cleared"
        
        return jsonify({
            "success": True,
            "message": message
        }), 200
    
    except Exception as e:
        logger.error(f"Error clearing cache: {e}")
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


# ============================================================================
# Error Handlers
# ============================================================================

@app.errorhandler(404)
def not_found(error):
    """Handle 404 errors"""
    return jsonify({
        "success": False,
        "error": "Endpoint not found"
    }), 404


@app.errorhandler(500)
def internal_error(error):
    """Handle 500 errors"""
    return jsonify({
        "success": False,
        "error": "Internal server error"
    }), 500


# ============================================================================
# Startup Initialization
# ============================================================================

def initialize_database_on_startup():
    """
    Initialize database on server startup
    Sync from Supabase if local database is empty
    """
    try:
        logger.info("="*60)
        logger.info("🔄 Checking local face database...")
        
        # Check face_database folder
        face_db_path = settings.DATABASE_FOLDER
        
        # Count existing images
        has_images = any(
            f.name.lower().endswith(('.jpg', '.jpeg', '.png'))
            for f in face_db_path.rglob('*')
            if f.is_file()
        )
        
        if has_images:
            logger.info(f"✅ Local database found with images")
            return
        
        # Database is empty - try to sync from Supabase
        if supabase_service.is_enabled():
            logger.info(f"📥 Local database empty - syncing from Supabase...")
            
            # Try to sync default class database
            # You can add class_id from environment variable or config
            default_class_id = "default"  # or from settings
            
            student_count, message = supabase_service.sync_class_students(
                default_class_id, 
                face_db_path
            )
            
            if student_count > 0:
                logger.info(f"✅ {message}")
                # Reload embeddings
                face_service.embedding_cache._load_all_embeddings()
            else:
                logger.warning(f"⚠️  No students synced from Supabase")
        else:
            logger.warning(f"⚠️  Local database empty and Supabase not enabled")
        
        logger.info("="*60)
    
    except Exception as e:
        logger.error(f"❌ Error initializing database: {e}")


# ============================================================================
# Main Entry Point
# ============================================================================

if __name__ == '__main__':
    # Initialize database on startup
    initialize_database_on_startup()
    
    logger.info("="*60)
    logger.info("✅ All services initialized successfully")
    logger.info(f"🌐 Starting Flask server on {settings.FLASK_HOST}:{settings.FLASK_PORT}")
    logger.info("="*60)
    
    app.run(
        host=settings.FLASK_HOST,
        port=settings.FLASK_PORT,
        debug=settings.FLASK_DEBUG
    )
