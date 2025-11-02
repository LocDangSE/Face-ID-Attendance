"""
Automatic Face Database Sync Tool
Downloads face images from Supabase to local storage for DeepFace recognition
Runs as a background process or on-demand
"""

import os
import sys
import logging
from pathlib import Path
from typing import List, Dict, Tuple
from dotenv import load_dotenv
from supabase import create_client, Client
import concurrent.futures
from datetime import datetime

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Load environment variables
load_dotenv()

# Configuration
SUPABASE_URL = os.getenv('SUPABASE_URL', '')
SUPABASE_KEY = os.getenv('SUPABASE_KEY', '')
SUPABASE_BUCKET = os.getenv('SUPABASE_BUCKET', 'student-photos')
DATABASE_FOLDER = 'face_database'
MAX_WORKERS = 5  # Parallel downloads for speed


class FaceDatabaseSyncer:
    """Sync face images from Supabase to local storage"""
    
    def __init__(self):
        """Initialize Supabase client"""
        if not SUPABASE_URL or not SUPABASE_KEY:
            raise ValueError("❌ Supabase credentials not configured in .env file")
        
        self.client: Client = create_client(SUPABASE_URL, SUPABASE_KEY)
        self.bucket = SUPABASE_BUCKET
        self.db_folder = DATABASE_FOLDER
        
        # Ensure database folder exists
        os.makedirs(self.db_folder, exist_ok=True)
        
        logger.info(f"✅ Connected to Supabase (bucket: {self.bucket})")
    
    def get_local_classes(self) -> List[str]:
        """Get list of class folders in local database"""
        try:
            classes = [
                d for d in os.listdir(self.db_folder)
                if os.path.isdir(os.path.join(self.db_folder, d))
                and d != '__pycache__'
            ]
            return classes
        except Exception as e:
            logger.error(f"Error reading local classes: {e}")
            return []
    
    def get_supabase_classes(self) -> List[str]:
        """Get list of class folders in Supabase bucket"""
        try:
            # The student-photos bucket uses format: students/{student_id}/ 
            # Not class-based structure, so we get classes from local database instead
            # This function returns empty for student-photos bucket
            
            # However, for class-based buckets, list folders at root
            try:
                items = self.client.storage.from_(self.bucket).list()
                
                if not items or len(items) == 0:
                    logger.info("📦 Bucket is empty or uses different structure")
                    return []
                
                # Filter for folders (class IDs) - must be UUIDs
                import re
                uuid_pattern = re.compile(r'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$', re.I)
                
                classes = []
                for item in items:
                    if item and isinstance(item, dict):
                        name = item.get('name', '')
                        if name and uuid_pattern.match(name):
                            classes.append(name)
                
                logger.info(f"📦 Found {len(classes)} classes in Supabase")
                return classes
            except:
                return []
                
        except Exception as e:
            logger.error(f"❌ Error listing Supabase classes: {e}")
            return []
    
    def check_class_status(self, class_id: str) -> Dict:
        """Check if class has local images"""
        class_path = os.path.join(self.db_folder, class_id)
        
        if not os.path.exists(class_path):
            return {
                'class_id': class_id,
                'status': 'missing',
                'local_images': 0,
                'needs_sync': True
            }
        
        # Count image files
        image_count = 0
        for root, dirs, files in os.walk(class_path):
            image_count += len([
                f for f in files
                if f.lower().endswith(('.jpg', '.jpeg', '.png'))
            ])
        
        return {
            'class_id': class_id,
            'status': 'exists' if image_count > 0 else 'empty',
            'local_images': image_count,
            'needs_sync': image_count == 0
        }
    
    def download_student_face(
        self,
        class_id: str,
        student_id: str
    ) -> Tuple[bool, str]:
        """Download a single student's face image from Supabase"""
        try:
            # Try multiple possible paths
            possible_paths = [
                f"{class_id}/{student_id}/face.jpg",
                f"{class_id}/{student_id}/{student_id}.jpg",
                f"students/{student_id}.jpg"  # Fallback to students folder
            ]
            
            image_data = None
            successful_path = None
            
            for remote_path in possible_paths:
                try:
                    image_data = self.client.storage.from_(self.bucket).download(remote_path)
                    successful_path = remote_path
                    break
                except:
                    continue
            
            if not image_data:
                return False, f"Image not found in Supabase for {student_id}"
            
            # Create local directory
            student_folder = os.path.join(self.db_folder, class_id, student_id)
            os.makedirs(student_folder, exist_ok=True)
            
            # Save image
            local_path = os.path.join(student_folder, f"{student_id}.jpg")
            with open(local_path, 'wb') as f:
                f.write(image_data)
            
            return True, local_path
        
        except Exception as e:
            return False, str(e)
    
    def sync_class(self, class_id: str) -> Dict:
        """Sync all students in a class from Supabase"""
        logger.info(f"📥 Syncing class: {class_id}")
        
        try:
            # List students in Supabase
            items = self.client.storage.from_(self.bucket).list(f"{class_id}/")
            
            # Filter student folders
            student_ids = [
                item['name'] for item in items
                if item.get('id') and '/' not in item.get('name', '')
            ]
            
            if not student_ids:
                logger.warning(f"  ⚠️  No students found in Supabase for class {class_id}")
                return {
                    'class_id': class_id,
                    'total': 0,
                    'success': 0,
                    'failed': 0
                }
            
            logger.info(f"  Found {len(student_ids)} students")
            
            # Download images in parallel for speed
            results = {
                'class_id': class_id,
                'total': len(student_ids),
                'success': 0,
                'failed': 0,
                'students': []
            }
            
            with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
                # Submit all download tasks
                future_to_student = {
                    executor.submit(self.download_student_face, class_id, student_id): student_id
                    for student_id in student_ids
                }
                
                # Collect results
                for future in concurrent.futures.as_completed(future_to_student):
                    student_id = future_to_student[future]
                    try:
                        success, message = future.result()
                        if success:
                            results['success'] += 1
                            logger.info(f"    ✅ {student_id}")
                        else:
                            results['failed'] += 1
                            logger.warning(f"    ❌ {student_id}: {message}")
                        
                        results['students'].append({
                            'student_id': student_id,
                            'success': success,
                            'message': message
                        })
                    
                    except Exception as e:
                        results['failed'] += 1
                        logger.error(f"    ❌ {student_id}: {e}")
            
            logger.info(
                f"  ✨ Completed: {results['success']}/{results['total']} students synced"
            )
            return results
        
        except Exception as e:
            logger.error(f"  ❌ Error syncing class {class_id}: {e}")
            return {
                'class_id': class_id,
                'total': 0,
                'success': 0,
                'failed': 0,
                'error': str(e)
            }
    
    def sync_all_classes(self, force: bool = False) -> Dict:
        """Sync all classes from Supabase"""
        logger.info("="*60)
        logger.info("🚀 Starting Face Database Sync")
        logger.info("="*60)
        
        # Get classes from Supabase
        supabase_classes = self.get_supabase_classes()
        
        if not supabase_classes:
            logger.warning("No classes found in Supabase")
            return {'total': 0, 'synced': 0, 'skipped': 0}
        
        # Check which classes need syncing
        classes_to_sync = []
        for class_id in supabase_classes:
            status = self.check_class_status(class_id)
            
            if force or status['needs_sync']:
                classes_to_sync.append(class_id)
                logger.info(f"📌 Will sync: {class_id} (status: {status['status']})")
            else:
                logger.info(f"⏭️  Skip: {class_id} ({status['local_images']} images exist)")
        
        if not classes_to_sync:
            logger.info("✨ All classes are already synced!")
            return {
                'total': len(supabase_classes),
                'synced': 0,
                'skipped': len(supabase_classes)
            }
        
        # Sync classes
        logger.info(f"\n📦 Syncing {len(classes_to_sync)} classes...")
        
        results = {
            'total': len(supabase_classes),
            'synced': 0,
            'skipped': len(supabase_classes) - len(classes_to_sync),
            'classes': []
        }
        
        for class_id in classes_to_sync:
            class_result = self.sync_class(class_id)
            if class_result['success'] > 0:
                results['synced'] += 1
            results['classes'].append(class_result)
        
        # Summary
        logger.info("="*60)
        logger.info("✅ Sync Complete!")
        logger.info(f"Total classes: {results['total']}")
        logger.info(f"Synced: {results['synced']}")
        logger.info(f"Skipped: {results['skipped']}")
        logger.info("="*60)
        
        return results
    
    def sync_single_class(self, class_id: str) -> Dict:
        """Sync a specific class"""
        logger.info(f"🔄 Syncing single class: {class_id}")
        return self.sync_class(class_id)


def main():
    """Main entry point"""
    import argparse
    
    parser = argparse.ArgumentParser(
        description='Sync face images from Supabase to local storage'
    )
    parser.add_argument(
        '--class-id',
        help='Sync specific class only',
        default=None
    )
    parser.add_argument(
        '--force',
        action='store_true',
        help='Force sync even if images exist locally'
    )
    
    args = parser.parse_args()
    
    try:
        # Initialize syncer
        syncer = FaceDatabaseSyncer()
        
        # Sync
        if args.class_id:
            result = syncer.sync_single_class(args.class_id)
        else:
            result = syncer.sync_all_classes(force=args.force)
        
        return 0 if result else 1
    
    except Exception as e:
        logger.error(f"❌ Fatal error: {e}")
        import traceback
        traceback.print_exc()
        return 1


if __name__ == '__main__':
    sys.exit(main())
