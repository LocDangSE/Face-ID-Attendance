"""
Supabase Service
Clean integration for cloud storage management
"""

import os
import logging
from supabase import create_client, Client
from typing import Optional, Tuple, List, Dict
from pathlib import Path

from config.settings import settings

logger = logging.getLogger(__name__)


class SupabaseService:
    """
    Service for Supabase cloud storage operations
    Handles file uploads, downloads, and session management
    """
    
    def __init__(self):
        """Initialize Supabase service"""
        self.url = settings.SUPABASE_URL
        self.key = settings.SUPABASE_KEY
        self.bucket = settings.SUPABASE_BUCKET
        self.enabled = settings.SUPABASE_ENABLED
        self.client: Optional[Client] = None
        
        if self.enabled and self.url and self.key:
            try:
                self.client = create_client(self.url, self.key)
                logger.info(f"✅ Supabase client initialized (bucket: {self.bucket})")
            except Exception as e:
                logger.error(f"❌ Failed to initialize Supabase: {e}")
                self.enabled = False
        else:
            logger.info("ℹ️  Supabase storage disabled or not configured")
    
    def is_enabled(self) -> bool:
        """Check if Supabase is enabled and configured"""
        return self.enabled and self.client is not None
    
    def upload_file(
        self,
        local_path: str,
        remote_path: str,
        content_type: str = "image/jpeg"
    ) -> Tuple[bool, Optional[str], Optional[str]]:
        """
        Upload a file to Supabase Storage
        
        Args:
            local_path: Path to local file
            remote_path: Destination path in bucket (e.g., "students/uuid.jpg")
            content_type: MIME type of file
            
        Returns:
            Tuple of (success, public_url, error_message)
        """
        if not self.is_enabled():
            return False, None, "Supabase storage is not enabled"
        
        try:
            # Read file
            with open(local_path, 'rb') as f:
                file_data = f.read()
            
            # Delete existing file if present
            try:
                self.client.storage.from_(self.bucket).remove([remote_path])
            except:
                pass  # File doesn't exist, continue
            
            # Upload to Supabase
            response = self.client.storage.from_(self.bucket).upload(
                path=remote_path,
                file=file_data,
                file_options={
                    "content-type": content_type,
                    "upsert": "true"
                }
            )
            
            # Get public URL
            public_url = self.get_public_url(remote_path)
            
            logger.info(f"✅ Uploaded to Supabase: {remote_path}")
            return True, public_url, None
        
        except Exception as e:
            error_msg = f"Failed to upload to Supabase: {str(e)}"
            logger.error(f"❌ {error_msg}")
            return False, None, error_msg
    
    def download_file(
        self,
        remote_path: str,
        local_path: str
    ) -> Tuple[bool, Optional[str]]:
        """
        Download a file from Supabase Storage
        
        Args:
            remote_path: Path to file in bucket
            local_path: Local destination path
            
        Returns:
            Tuple of (success, error_message)
        """
        if not self.is_enabled():
            return False, "Supabase storage is not enabled"
        
        try:
            # Download file
            file_data = self.client.storage.from_(self.bucket).download(remote_path)
            
            if not file_data:
                return False, f"File not found: {remote_path}"
            
            # Save to local path
            Path(local_path).parent.mkdir(parents=True, exist_ok=True)
            with open(local_path, 'wb') as f:
                f.write(file_data)
            
            logger.info(f"✅ Downloaded from Supabase: {remote_path} -> {local_path}")
            return True, None
        
        except Exception as e:
            error_msg = f"Failed to download from Supabase: {str(e)}"
            logger.error(f"❌ {error_msg}")
            return False, error_msg
    
    def delete_file(self, remote_path: str) -> Tuple[bool, Optional[str]]:
        """
        Delete a file from Supabase Storage
        
        Args:
            remote_path: Path to file in bucket
            
        Returns:
            Tuple of (success, error_message)
        """
        if not self.is_enabled():
            return False, "Supabase storage is not enabled"
        
        try:
            self.client.storage.from_(self.bucket).remove([remote_path])
            logger.info(f"✅ Deleted from Supabase: {remote_path}")
            return True, None
        
        except Exception as e:
            error_msg = f"Failed to delete from Supabase: {str(e)}"
            logger.warning(f"⚠️  {error_msg}")
            return False, error_msg
    
    def get_public_url(self, remote_path: str) -> str:
        """
        Get public URL for a file in storage
        
        Args:
            remote_path: Path to file in bucket
            
        Returns:
            Public URL string
        """
        if not self.is_enabled():
            return ""
        
        try:
            public_url = self.client.storage.from_(self.bucket).get_public_url(remote_path)
            return public_url
        except Exception as e:
            logger.error(f"❌ Failed to get public URL: {e}")
            return ""
    
    def list_files(self, folder: str = "") -> List[Dict]:
        """
        List files in a folder
        
        Args:
            folder: Folder path in bucket
            
        Returns:
            List of file objects
        """
        if not self.is_enabled():
            return []
        
        try:
            files = self.client.storage.from_(self.bucket).list(folder)
            return files
        except Exception as e:
            logger.error(f"❌ Failed to list files: {e}")
            return []
    
    def save_student_face(
        self,
        local_path: str,
        student_id: str,
        filename: str
    ) -> Tuple[str, bool]:
        """
        Save student face image to Supabase
        
        Args:
            local_path: Path to local temporary file
            student_id: Student ID
            filename: Desired filename
            
        Returns:
            Tuple of (public_url, success)
            
        Raises:
            Exception: If Supabase is not enabled or upload fails
        """
        if not self.is_enabled():
            error_msg = "❌ Supabase storage is not enabled. Please configure SUPABASE_ENABLED=true in .env"
            logger.error(error_msg)
            raise Exception(error_msg)
        
        remote_path = f"students/{student_id}/{filename}"
        
        logger.info(f"📤 Uploading student face to Supabase: {remote_path}")
        success, public_url, error = self.upload_file(
            local_path=local_path,
            remote_path=remote_path,
            content_type="image/jpeg"
        )
        
        if success and public_url:
            logger.info(f"✅ Successfully saved to Supabase: {public_url}")
            return public_url, True
        else:
            error_msg = f"❌ FAILED to upload to Supabase: {error}"
            logger.error(error_msg)
            raise Exception(error_msg)
    
    def sync_class_students(
        self,
        class_id: str,
        local_folder: Path
    ) -> Tuple[int, str]:
        """
        Sync class database from Supabase to local storage
        Downloads all student images for a class
        
        Args:
            class_id: Class identifier
            local_folder: Local folder to save images
            
        Returns:
            Tuple of (student_count, message)
        """
        if not self.is_enabled():
            return 0, "Supabase not enabled"
        
        try:
            logger.info(f"📥 Syncing class {class_id} from Supabase...")
            
            # Create class folder
            local_folder.mkdir(parents=True, exist_ok=True)
            
            # List all entries in students/
            items = self.list_files("students/")
            
            IMAGE_EXTS = ('.jpg', '.jpeg', '.png')
            
            # Separate folder-style and root-level images
            folder_student_ids = [
                item.get('name', '') for item in items
                if item.get('name') and not item.get('name', '').lower().endswith(IMAGE_EXTS)
                and '.' not in item.get('name', '')
            ]
            
            root_images = [
                item.get('name', '') for item in items
                if item.get('name', '').lower().endswith(IMAGE_EXTS)
            ]
            
            if not folder_student_ids and not root_images:
                return 0, "No students found in Supabase"
            
            logger.info(f"  Found {len(folder_student_ids)} student folders and {len(root_images)} root images")
            
            success_count = 0
            
            # Download from student folders
            for student_id in folder_student_ids:
                try:
                    student_files = self.list_files(f"students/{student_id}/")
                    image_files = [
                        f for f in student_files
                        if f.get('name', '').lower().endswith(IMAGE_EXTS)
                    ]
                    
                    if not image_files:
                        continue
                    
                    # Download first image
                    image_file = image_files[0]['name']
                    remote_path = f"students/{student_id}/{image_file}"
                    
                    # Save directly in class folder with student ID as filename
                    local_path = str(local_folder / f"{student_id}.jpg")
                    
                    success, error = self.download_file(remote_path, local_path)
                    if success:
                        success_count += 1
                        logger.info(f"  ✅ Downloaded {student_id}.jpg")
                
                except Exception as e:
                    logger.error(f"  ❌ Error downloading student {student_id}: {e}")
            
            # Download root images
            for filename in root_images:
                try:
                    base = os.path.basename(filename)
                    name_wo_ext, _ = os.path.splitext(base)
                    # Use the full filename (without extension) as student_id - it should be a GUID
                    student_id = name_wo_ext
                    
                    remote_path = f"students/{filename}"
                    # Save directly in class folder with student ID as filename
                    local_path = str(local_folder / f"{student_id}.jpg")
                    
                    success, error = self.download_file(remote_path, local_path)
                    if success:
                        success_count += 1
                        logger.info(f"  ✅ Downloaded {student_id}.jpg from root")
                
                except Exception as e:
                    logger.error(f"  ❌ Error downloading root image {filename}: {e}")
            
            message = f"Synced {success_count} students from Supabase"
            logger.info(f"  ✅ {message}")
            return success_count, message
        
        except Exception as e:
            error_msg = f"Sync error: {str(e)}"
            logger.error(f"❌ {error_msg}")
            return 0, error_msg
