"""
Face Recognition API Service for Attendance System
Using DeepFace library for face detection and recognition
With Supabase cloud storage support
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
from deepface import DeepFace
import os
import uuid
import shutil
from datetime import datetime
from werkzeug.utils import secure_filename
import logging
from dotenv import load_dotenv
from supabase_storage import SupabaseStorage, HybridStorage
import concurrent.futures

# Load environment variables
load_dotenv()

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = Flask(__name__)
CORS(app)  # Enable CORS for all routes

# Configuration
UPLOAD_FOLDER = 'uploads'
TEMP_FOLDER = 'temp'
DATABASE_FOLDER = 'face_database'
ALLOWED_EXTENSIONS = {'png', 'jpg', 'jpeg'}
MAX_FILE_SIZE = 5 * 1024 * 1024  # 5MB

# Create necessary directories
for folder in [UPLOAD_FOLDER, TEMP_FOLDER, DATABASE_FOLDER]:
    os.makedirs(folder, exist_ok=True)

# Initialize Supabase Storage
supabase_storage = SupabaseStorage(
    url=os.getenv('SUPABASE_URL', ''),
    key=os.getenv('SUPABASE_KEY', ''),
    bucket=os.getenv('SUPABASE_BUCKET', 'student-photos'),
    enabled=os.getenv('SUPABASE_ENABLED', 'false').lower() == 'true'
)

# Initialize Hybrid Storage (Supabase + Local fallback)
hybrid_storage = HybridStorage(
    supabase_storage=supabase_storage,
    local_folder=DATABASE_FOLDER
)

# DeepFace Configuration
DEEPFACE_MODEL = 'Facenet512'  # Options: VGG-Face, Facenet, Facenet512, OpenFace, DeepFace, DeepID, Dlib, ArcFace
DISTANCE_METRIC = 'cosine'  # Options: cosine, euclidean, euclidean_l2
DETECTOR_BACKEND = 'opencv'  # Options: opencv, ssd, dlib, mtcnn, retinaface
CONFIDENCE_THRESHOLD = 0.6  # Lower distance = more similar (0.0 to 1.0)


def allowed_file(filename):
    """Check if file extension is allowed"""
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS


def save_uploaded_file(file, folder=TEMP_FOLDER):
    """Save uploaded file with unique name"""
    try:
        if not file or file.filename == '':
            return None, "No file provided"
        
        if not allowed_file(file.filename):
            return None, f"File type not allowed. Use: {', '.join(ALLOWED_EXTENSIONS)}"
        
        # Generate unique filename
        file_ext = secure_filename(file.filename).rsplit('.', 1)[1].lower()
        unique_filename = f"{uuid.uuid4()}.{file_ext}"
        file_path = os.path.join(folder, unique_filename)
        
        file.save(file_path)
        logger.info(f"File saved: {file_path}")
        return file_path, None
    
    except Exception as e:
        logger.error(f"Error saving file: {str(e)}")
        return None, str(e)


def cleanup_temp_file(file_path):
    """Delete temporary file"""
    try:
        if file_path and os.path.exists(file_path):
            os.remove(file_path)
            logger.info(f"Temp file deleted: {file_path}")
    except Exception as e:
        logger.warning(f"Could not delete temp file {file_path}: {str(e)}")


def auto_sync_class_database(class_id):
    """
    Automatically sync class database from Supabase if empty
    Downloads face images from Supabase to local storage for DeepFace
    Returns: (success, message, student_count)
    """
    try:
        class_db_path = os.path.join(DATABASE_FOLDER, class_id)
        
        # Check if database has images
        image_count = 0
        if os.path.exists(class_db_path):
            for root, dirs, files in os.walk(class_db_path):
                image_count += len([
                    f for f in files
                    if f.lower().endswith(('.jpg', '.jpeg', '.png'))
                ])
        
        # If images exist, no need to sync
        if image_count > 0:
            logger.info(f"✅ Class {class_id} has {image_count} images locally")
            return True, f"{image_count} students ready", image_count
        
        # Check if Supabase is enabled
        if not supabase_storage.is_enabled():
            return False, "Supabase not enabled and no local images found", 0
        
        logger.info(f"📥 Auto-syncing class {class_id} from Supabase...")
        
        # Create class folder
        os.makedirs(class_db_path, exist_ok=True)
        
        # List all entries in Supabase under students/ (can contain folders and files)
        items = supabase_storage.client.storage.from_(supabase_storage.bucket).list("students/")

        IMAGE_EXTS = ('.jpg', '.jpeg', '.png')

        # Separate folder-style student entries and root-level images
        folder_student_ids = [
            item.get('name', '') for item in items
            if item.get('name') and not item.get('name', '').lower().endswith(IMAGE_EXTS) and '.' not in item.get('name', '')
        ]

        root_images = [
            item.get('name', '') for item in items
            if item.get('name', '').lower().endswith(IMAGE_EXTS)
        ]

        if not folder_student_ids and not root_images:
            return False, "No students or images found in Supabase bucket (students/)", 0

        logger.info(f"  Found {len(folder_student_ids)} student folders and {len(root_images)} root images in Supabase")
        
        # Download images in parallel for speed
        success_count = 0
        
        def download_student_from_folder(student_id):
            """Download a student's face image from their folder under students/{student_id}/"""
            try:
                # List files in student folder
                student_files = supabase_storage.client.storage.from_(supabase_storage.bucket).list(f"students/{student_id}/")

                # Find image files
                image_files = [
                    f for f in student_files
                    if f.get('name', '').lower().endswith(IMAGE_EXTS)
                ]

                if not image_files:
                    logger.warning(f"    No images found in folder for student {student_id}")
                    return False

                # Pick the last entry (supabase returns sorted by name asc). Adjust if needed.
                image_file = image_files[-1]['name']

                # Download the image
                image_path = f"students/{student_id}/{image_file}"
                logger.info(f"    Downloading (folder): {image_path}")
                image_data = supabase_storage.client.storage.from_(supabase_storage.bucket).download(image_path)

                if not image_data:
                    logger.warning(f"    Failed to download {image_path}")
                    return False

                # Save locally
                student_folder = os.path.join(class_db_path, student_id)
                os.makedirs(student_folder, exist_ok=True)

                local_path = os.path.join(student_folder, f"{student_id}.jpg")
                with open(local_path, 'wb') as f:
                    f.write(image_data)

                logger.info(f"    ✅ Downloaded {student_id} → {local_path}")
                return True
            except Exception as e:
                logger.error(f"    ❌ Error downloading from folder {student_id}: {str(e)}")
                return False

        def download_student_from_root(filename):
            """Download a root-level image under students/ and derive student_id from filename."""
            try:
                base = os.path.basename(filename)
                name_wo_ext, _ = os.path.splitext(base)
                # Derive student_id: either full name before first underscore, or entire name without extension
                student_id = name_wo_ext.split('_')[0] if '_' in name_wo_ext else name_wo_ext

                image_path = f"students/{filename}"
                logger.info(f"    Downloading (root): {image_path} → student {student_id}")
                image_data = supabase_storage.client.storage.from_(supabase_storage.bucket).download(image_path)

                if not image_data:
                    logger.warning(f"    Failed to download {image_path}")
                    return False

                # Save locally
                student_folder = os.path.join(class_db_path, student_id)
                os.makedirs(student_folder, exist_ok=True)

                local_path = os.path.join(student_folder, f"{student_id}.jpg")
                with open(local_path, 'wb') as f:
                    f.write(image_data)

                logger.info(f"    ✅ Downloaded {student_id} (root) → {local_path}")
                return True
            except Exception as e:
                logger.error(f"    ❌ Error downloading root image {filename}: {str(e)}")
                return False
        
        # Use thread pool for parallel downloads (faster)
        with concurrent.futures.ThreadPoolExecutor(max_workers=5) as executor:
            results = []
            if folder_student_ids:
                results += list(executor.map(download_student_from_folder, folder_student_ids))
            if root_images:
                results += list(executor.map(download_student_from_root, root_images))
            success_count = sum(1 for r in results if r)
        
        if success_count > 0:
            total_sources = len(folder_student_ids) + len(root_images)
            logger.info(f"  ✅ Synced {success_count}/{total_sources} student images")
            return True, f"Auto-synced {success_count} students from Supabase", success_count
        else:
            return False, "Failed to download any student images", 0
    
    except Exception as e:
        logger.error(f"❌ Auto-sync error for class {class_id}: {e}")
        return False, f"Auto-sync error: {str(e)}", 0


# ============================================================================
# ENDPOINT 1: Health Check
# ============================================================================

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        "status": "healthy",
        "service": "Face Recognition API",
        "model": DEEPFACE_MODEL,
        "storage": {
            "supabase": {
                "enabled": supabase_storage.is_enabled(),
                "bucket": supabase_storage.bucket if supabase_storage.is_enabled() else None
            },
            "local": {
                "enabled": True,
                "path": DATABASE_FOLDER
            }
        },
        "timestamp": datetime.utcnow().isoformat()
    }), 200


# ============================================================================
# ENDPOINT 2: Detect Faces in Image
# ============================================================================

@app.route('/api/face/detect', methods=['POST'])
def detect_faces():
    """
    Detect faces in an uploaded image
    Returns: List of detected face regions and count
    """
    temp_file = None
    try:
        # Validate image upload
        if 'image' not in request.files:
            return jsonify({
                "success": False,
                "error": "No image file provided",
                "detectedFaces": []
            }), 400
        
        # Save uploaded file
        temp_file, error = save_uploaded_file(request.files['image'])
        if error:
            return jsonify({
                "success": False,
                "error": error,
                "detectedFaces": []
            }), 400
        
        # Detect faces using DeepFace
        logger.info(f"Detecting faces in: {temp_file}")
        faces = DeepFace.extract_faces(
            img_path=temp_file,
            detector_backend=DETECTOR_BACKEND,
            enforce_detection=False,
            align=True
        )
        
        if not faces or len(faces) == 0:
            return jsonify({
                "success": False,
                "message": "No faces detected in the image",
                "detectedFaces": []
            }), 200
        
        # Format response
        detected_faces = []
        for idx, face_obj in enumerate(faces):
            if face_obj['confidence'] > 0:  # Only include faces with positive confidence
                detected_faces.append({
                    "faceId": str(uuid.uuid4()),
                    "confidence": round(face_obj['confidence'], 4),
                    "region": {
                        "x": int(face_obj['facial_area']['x']),
                        "y": int(face_obj['facial_area']['y']),
                        "width": int(face_obj['facial_area']['w']),
                        "height": int(face_obj['facial_area']['h'])
                    }
                })
        
        logger.info(f"Detected {len(detected_faces)} face(s)")
        
        return jsonify({
            "success": True,
            "message": f"Detected {len(detected_faces)} face(s)",
            "detectedFaces": detected_faces,
            "totalFaces": len(detected_faces)
        }), 200
    
    except Exception as e:
        logger.error(f"Error in detect_faces: {str(e)}")
        return jsonify({
            "success": False,
            "error": f"Face detection error: {str(e)}",
            "detectedFaces": []
        }), 500
    
    finally:
        cleanup_temp_file(temp_file)


# ============================================================================
# ENDPOINT 3: Register Student Face (Add to Database)
# ============================================================================

@app.route('/api/face/register', methods=['POST'])
def register_face():
    """
    Register a student's face to the database
    Required: image file, studentId
    Returns: Registration status
    """
    temp_file = None
    try:
        # Validate request
        if 'image' not in request.files:
            return jsonify({
                "success": False,
                "error": "No image file provided"
            }), 400
        
        student_id = request.form.get('studentId')
        if not student_id:
            return jsonify({
                "success": False,
                "error": "studentId is required"
            }), 400
        
        # Save uploaded file
        temp_file, error = save_uploaded_file(request.files['image'])
        if error:
            return jsonify({
                "success": False,
                "error": error
            }), 400
        
        # Verify face exists in image
        logger.info(f"Verifying face for student: {student_id}")
        faces = DeepFace.extract_faces(
            img_path=temp_file,
            detector_backend=DETECTOR_BACKEND,
            enforce_detection=False
        )
        
        if not faces or len(faces) == 0:
            return jsonify({
                "success": False,
                "error": "No face detected in the image"
            }), 400
        
        if len(faces) > 1:
            return jsonify({
                "success": False,
                "error": "Multiple faces detected. Please upload image with single face"
            }), 400
        
        # Save face image to Supabase cloud storage (no local fallback)
        file_ext = temp_file.rsplit('.', 1)[1]
        face_filename = f"{student_id}_{datetime.utcnow().strftime('%Y%m%d_%H%M%S')}.{file_ext}"
        
        try:
            face_url_or_path, is_cloud = hybrid_storage.save_face_image(
                local_path=temp_file,
                student_id=student_id,
                filename=face_filename
            )
            
            storage_type = "cloud (Supabase)"
            logger.info(f"✅ Face registered for student {student_id} in Supabase cloud: {face_url_or_path}")
            
            return jsonify({
                "success": True,
                "message": "Face registered successfully to Supabase cloud storage",
                "studentId": student_id,
                "facePath": face_url_or_path,
                "storageType": storage_type,
                "isCloudStorage": is_cloud,
                "confidence": round(faces[0]['confidence'], 4)
            }), 200
        
        except Exception as storage_error:
            logger.error(f"❌ Failed to upload to Supabase for student {student_id}: {str(storage_error)}")
            return jsonify({
                "success": False,
                "error": f"Failed to upload to Supabase cloud storage: {str(storage_error)}"
            }), 500
    
    except Exception as e:
        logger.error(f"Error in register_face: {str(e)}")
        return jsonify({
            "success": False,
            "error": f"Registration error: {str(e)}"
        }), 500
    
    finally:
        cleanup_temp_file(temp_file)


# ============================================================================
# ENDPOINT 4: Recognize Face (Identify Student)
# ============================================================================

@app.route('/api/face/recognize', methods=['POST'])
def recognize_face():
    """
    Recognize student from uploaded image against class database
    Required: image file, classId
    Returns: List of recognized students with confidence scores
    """
    temp_file = None
    try:
        # Validate request
        if 'image' not in request.files:
            return jsonify({
                "success": False,
                "error": "No image file provided",
                "recognizedStudents": []
            }), 400
        
        class_id = request.form.get('classId')
        if not class_id:
            return jsonify({
                "success": False,
                "error": "classId is required",
                "recognizedStudents": []
            }), 400
        
        # Save uploaded file
        temp_file, error = save_uploaded_file(request.files['image'])
        if error:
            return jsonify({
                "success": False,
                "error": error,
                "recognizedStudents": []
            }), 400
        
        # Check if class database exists and auto-sync if needed
        class_db_path = os.path.join(DATABASE_FOLDER, class_id)
        if not os.path.exists(class_db_path):
            # Try alternate format
            class_db_path = os.path.join(DATABASE_FOLDER, f"class_{class_id}")
        
        # Auto-sync from Supabase if database is empty
        if not os.path.exists(class_db_path) or not any(
            f.lower().endswith(('.jpg', '.jpeg', '.png'))
            for root, dirs, files in os.walk(class_db_path)
            for f in files
        ):
            logger.info(f"🔄 Class database empty, attempting auto-sync from Supabase...")
            sync_success, sync_message, student_count = auto_sync_class_database(class_id)
            
            if not sync_success or student_count == 0:
                return jsonify({
                    "success": False,
                    "error": f"No students registered for this class. {sync_message}",
                    "recognizedStudents": [],
                    "hint": "Please run 'Setup Class Database' from the frontend first"
                }), 400
            
            logger.info(f"✅ Auto-sync complete: {sync_message}")
            # Update path after sync
            class_db_path = os.path.join(DATABASE_FOLDER, class_id)
        
        # Detect faces in uploaded image
        logger.info(f"Detecting faces for class {class_id}")
        faces = DeepFace.extract_faces(
            img_path=temp_file,
            detector_backend=DETECTOR_BACKEND,
            enforce_detection=False
        )
        
        if not faces or len(faces) == 0:
            return jsonify({
                "success": False,
                "message": "No faces detected in the image",
                "recognizedStudents": []
            }), 200
        
        # Log class database contents
        logger.info(f"📂 Class database path: {class_db_path}")
        if os.path.exists(class_db_path):
            student_folders = [d for d in os.listdir(class_db_path) if os.path.isdir(os.path.join(class_db_path, d))]
            logger.info(f"📋 Students in database: {student_folders}")
        else:
            logger.error(f"❌ Class database path does not exist!")
        
        # Recognize each detected face
        recognized_students = []
        
        for face_idx, face_obj in enumerate(faces):
            try:
                # Perform face recognition against class database
                logger.info(f"Recognizing face {face_idx + 1}/{len(faces)}")
                
                result = DeepFace.find(
                    img_path=temp_file,
                    db_path=class_db_path,
                    model_name=DEEPFACE_MODEL,
                    distance_metric=DISTANCE_METRIC,
                    detector_backend=DETECTOR_BACKEND,
                    enforce_detection=False,
                    silent=True
                )
                
                # Process results
                logger.info(f"DeepFace.find returned {len(result)} result(s)")
                if result and len(result) > 0 and not result[0].empty:
                    df = result[0]
                    logger.info(f"📊 Found {len(df)} potential matches")
                    
                    # Log top 3 matches for debugging
                    for idx in range(min(3, len(df))):
                        match = df.iloc[idx]
                        logger.info(f"  Match {idx+1}: distance={match['distance']:.4f}, path={match['identity']}")
                    
                    # Get best match (lowest distance)
                    best_match = df.iloc[0]
                    distance = float(best_match['distance'])
                    
                    logger.info(f"🎯 Best match distance: {distance:.4f}, Threshold: {CONFIDENCE_THRESHOLD}")
                    
                    # Check if distance is below threshold
                    if distance <= CONFIDENCE_THRESHOLD:
                        # Extract student ID from path
                        identity_path = str(best_match['identity'])
                        student_id = os.path.basename(os.path.dirname(identity_path))
                        
                        # Calculate confidence (inverse of distance)
                        confidence = 1.0 - distance
                        
                        recognized_students.append({
                            "studentId": student_id,
                            "confidence": round(confidence, 4),
                            "distance": round(distance, 4),
                            "matchedImage": identity_path,
                            "faceRegion": {
                                "x": int(face_obj['facial_area']['x']),
                                "y": int(face_obj['facial_area']['y']),
                                "width": int(face_obj['facial_area']['w']),
                                "height": int(face_obj['facial_area']['h'])
                            }
                        })
                        
                        logger.info(f"✅ Recognized: {student_id} (confidence: {confidence:.4f})")
                    else:
                        logger.warning(f"❌ Distance {distance:.4f} exceeds threshold {CONFIDENCE_THRESHOLD}")
                else:
                    logger.warning(f"⚠️ No matches found in database or empty result")
            
            except Exception as face_error:
                logger.error(f"❌ Could not recognize face {face_idx + 1}: {str(face_error)}")
                import traceback
                logger.error(traceback.format_exc())
                continue
        
        # Return results
        if recognized_students:
            return jsonify({
                "success": True,
                "message": f"Recognized {len(recognized_students)} student(s)",
                "recognizedStudents": recognized_students,
                "totalFacesDetected": len(faces),
                "totalRecognized": len(recognized_students)
            }), 200
        else:
            return jsonify({
                "success": False,
                "message": "No students recognized",
                "recognizedStudents": [],
                "totalFacesDetected": len(faces),
                "totalRecognized": 0
            }), 200
    
    except Exception as e:
        logger.error(f"Error in recognize_face: {str(e)}")
        return jsonify({
            "success": False,
            "error": f"Recognition error: {str(e)}",
            "recognizedStudents": []
        }), 500
    
    finally:
        cleanup_temp_file(temp_file)


# ============================================================================
# ENDPOINT 5: Setup Class Database (Bulk Import)
# ============================================================================

@app.route('/api/face/class/setup', methods=['POST'])
def setup_class_database():
    """
    Setup face database for a class with multiple students
    Required: classId, students array with studentId and image
    Returns: Setup status with success/failure count
    """
    try:
        class_id = request.form.get('classId')
        if not class_id:
            return jsonify({
                "success": False,
                "error": "classId is required"
            }), 400
        
        # Create class database folder (without "class_" prefix for consistency)
        class_db_path = os.path.join(DATABASE_FOLDER, class_id)
        os.makedirs(class_db_path, exist_ok=True)
        
        results = {
            "classId": class_id,
            "totalStudents": 0,
            "successCount": 0,
            "failureCount": 0,
            "students": []
        }
        
        # Process each student
        files = request.files
        for key in files:
            if key.startswith('student_'):
                student_id = key.replace('student_', '')
                results["totalStudents"] += 1
                
                try:
                    file = files[key]
                    
                    # Save to temporary location
                    temp_file, error = save_uploaded_file(file)
                    if error:
                        results["failureCount"] += 1
                        results["students"].append({
                            "studentId": student_id,
                            "success": False,
                            "error": error
                        })
                        continue
                    
                    # Verify face
                    faces = DeepFace.extract_faces(
                        img_path=temp_file,
                        detector_backend=DETECTOR_BACKEND,
                        enforce_detection=False
                    )
                    
                    if not faces or len(faces) == 0:
                        cleanup_temp_file(temp_file)
                        results["failureCount"] += 1
                        results["students"].append({
                            "studentId": student_id,
                            "success": False,
                            "error": "No face detected"
                        })
                        continue
                    
                    # Save face to Supabase cloud storage
                    file_ext = temp_file.rsplit('.', 1)[1]
                    face_filename = f"{student_id}.{file_ext}"
                    
                    try:
                        # Upload to Supabase (will throw exception if fails)
                        face_url_or_path, is_cloud = hybrid_storage.save_face_image(
                            local_path=temp_file,
                            student_id=student_id,
                            filename=face_filename
                        )
                        
                        # Also save local copy for DeepFace recognition (DeepFace needs local files)
                        student_folder = os.path.join(class_db_path, student_id)
                        os.makedirs(student_folder, exist_ok=True)
                        local_face_path = os.path.join(student_folder, face_filename)
                        shutil.copy2(temp_file, local_face_path)
                        
                        cleanup_temp_file(temp_file)
                        
                        results["successCount"] += 1
                        results["students"].append({
                            "studentId": student_id,
                            "success": True,
                            "facePath": face_url_or_path,
                            "localPath": local_face_path,
                            "isCloudStorage": is_cloud
                        })
                        
                        logger.info(f"✅ Added student {student_id} to class {class_id} (Supabase + local for DeepFace)")
                    
                    except Exception as storage_error:
                        cleanup_temp_file(temp_file)
                        results["failureCount"] += 1
                        results["students"].append({
                            "studentId": student_id,
                            "success": False,
                            "error": f"Supabase upload failed: {str(storage_error)}"
                        })
                        logger.error(f"❌ Failed to upload student {student_id} to Supabase: {str(storage_error)}")
                
                except Exception as e:
                    results["failureCount"] += 1
                    results["students"].append({
                        "studentId": student_id,
                        "success": False,
                        "error": str(e)
                    })
        
        return jsonify({
            "success": results["successCount"] > 0,
            "message": f"Setup complete: {results['successCount']}/{results['totalStudents']} students added",
            "data": results
        }), 200
    
    except Exception as e:
        logger.error(f"Error in setup_class_database: {str(e)}")
        return jsonify({
            "success": False,
            "error": f"Setup error: {str(e)}"
        }), 500


# ============================================================================
# ENDPOINT 6: Delete Student from Class Database
# ============================================================================

@app.route('/api/face/class/<class_id>/student/<student_id>', methods=['DELETE'])
def delete_student_from_class(class_id, student_id):
    """
    Remove a student's face data from class database
    """
    try:
        # Try both formats
        student_folder = os.path.join(DATABASE_FOLDER, class_id, student_id)
        if not os.path.exists(student_folder):
            student_folder = os.path.join(DATABASE_FOLDER, f"class_{class_id}", student_id)
        
        if not os.path.exists(student_folder):
            return jsonify({
                "success": False,
                "error": "Student not found in class database"
            }), 404
        
        # Delete student folder
        shutil.rmtree(student_folder)
        logger.info(f"Deleted student {student_id} from class {class_id}")
        
        return jsonify({
            "success": True,
            "message": "Student removed from class database"
        }), 200
    
    except Exception as e:
        logger.error(f"Error deleting student: {str(e)}")
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


# ============================================================================
# ENDPOINT 7: Get Class Database Info
# ============================================================================

@app.route('/api/face/class/<class_id>/info', methods=['GET'])
def get_class_info(class_id):
    """
    Get information about class face database
    """
    try:
        # Try both formats
        class_db_path = os.path.join(DATABASE_FOLDER, class_id)
        if not os.path.exists(class_db_path):
            class_db_path = os.path.join(DATABASE_FOLDER, f"class_{class_id}")
        
        if not os.path.exists(class_db_path):
            return jsonify({
                "success": False,
                "error": "Class database not found"
            }), 404
        
        # Count students
        students = [d for d in os.listdir(class_db_path) 
                   if os.path.isdir(os.path.join(class_db_path, d))]
        
        return jsonify({
            "success": True,
            "classId": class_id,
            "totalStudents": len(students),
            "students": students,
            "databasePath": class_db_path
        }), 200
    
    except Exception as e:
        logger.error(f"Error getting class info: {str(e)}")
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500


# ============================================================================
# ENDPOINT 8: Verify Face Match (1:1 Verification)
# ============================================================================

@app.route('/api/face/verify', methods=['POST'])
def verify_face():
    """
    Verify if two faces belong to the same person
    Required: image1, image2
    """
    temp_file1 = None
    temp_file2 = None
    try:
        if 'image1' not in request.files or 'image2' not in request.files:
            return jsonify({
                "success": False,
                "error": "Both image1 and image2 are required"
            }), 400
        
        # Save both images
        temp_file1, error1 = save_uploaded_file(request.files['image1'])
        if error1:
            return jsonify({"success": False, "error": error1}), 400
        
        temp_file2, error2 = save_uploaded_file(request.files['image2'])
        if error2:
            cleanup_temp_file(temp_file1)
            return jsonify({"success": False, "error": error2}), 400
        
        # Verify faces
        result = DeepFace.verify(
            img1_path=temp_file1,
            img2_path=temp_file2,
            model_name=DEEPFACE_MODEL,
            distance_metric=DISTANCE_METRIC,
            detector_backend=DETECTOR_BACKEND,
            enforce_detection=False
        )
        
        return jsonify({
            "success": True,
            "verified": result['verified'],
            "distance": round(result['distance'], 4),
            "threshold": result['threshold'],
            "model": result['model'],
            "similarity": round(1.0 - result['distance'], 4)
        }), 200
    
    except Exception as e:
        logger.error(f"Error in verify_face: {str(e)}")
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500
    
    finally:
        cleanup_temp_file(temp_file1)
        cleanup_temp_file(temp_file2)


# ============================================================================
# Error Handlers
# ============================================================================

@app.errorhandler(404)
def not_found(error):
    return jsonify({
        "success": False,
        "error": "Endpoint not found"
    }), 404


@app.errorhandler(500)
def internal_error(error):
    return jsonify({
        "success": False,
        "error": "Internal server error"
    }), 500


# ============================================================================
# Main
# ============================================================================

if __name__ == '__main__':
    logger.info("="*60)
    logger.info("Face Recognition API Service Starting...")
    logger.info(f"Model: {DEEPFACE_MODEL}")
    logger.info(f"Distance Metric: {DISTANCE_METRIC}")
    logger.info(f"Detector: {DETECTOR_BACKEND}")
    logger.info(f"Confidence Threshold: {CONFIDENCE_THRESHOLD}")
    logger.info("-"*60)
    logger.info(f"Storage Configuration:")
    if supabase_storage.is_enabled():
        logger.info(f"  ✅ Supabase Cloud Storage: ENABLED")
        logger.info(f"     Bucket: {supabase_storage.bucket}")
    else:
        logger.info(f"  ℹ️  Supabase Cloud Storage: DISABLED (using local storage)")
    logger.info(f"  💾 Local Storage: {DATABASE_FOLDER}")
    logger.info("="*60)
    
    app.run(
        host='0.0.0.0',  # Allow external connections
        port=5000,
        debug=True
    )
