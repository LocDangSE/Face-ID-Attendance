"""
Supabase Storage Service for Face Recognition API
Handles image uploads to Supabase cloud storage
"""

import os
import logging
from supabase import create_client, Client
from typing import Optional, Tuple

logger = logging.getLogger(__name__)


class SupabaseStorage:
    """
    Service for uploading and managing face images in Supabase Storage
    """
    
    def __init__(
        self,
        url: str,
        key: str,
        bucket: str = "student-photos",
        enabled: bool = False
    ):
        """
        Initialize Supabase storage service
        
        Args:
            url: Supabase project URL
            key: Supabase anon/public key
            bucket: Storage bucket name
            enabled: Whether Supabase is enabled
        """
        self.enabled = enabled
        self.bucket = bucket
        self.url = url
        self.key = key
        self.client: Optional[Client] = None
        
        if self.enabled and url and key:
            try:
                self.client = create_client(url, key)
                logger.info(f"✅ Supabase client initialized (bucket: {bucket})")
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
            
            # Delete existing file if present (Supabase doesn't auto-overwrite)
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
            # Get public URL
            public_url = self.client.storage.from_(self.bucket).get_public_url(remote_path)
            return public_url
        except Exception as e:
            logger.error(f"❌ Failed to get public URL: {e}")
            return ""
    
    def list_files(self, folder: str = "") -> list:
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


class HybridStorage:
    """
    Hybrid storage that uses Supabase when available, falls back to local
    """
    
    def __init__(self, supabase_storage: SupabaseStorage, local_folder: str):
        """
        Initialize hybrid storage
        
        Args:
            supabase_storage: SupabaseStorage instance
            local_folder: Local folder for file storage fallback
        """
        self.supabase = supabase_storage
        self.local_folder = local_folder
        
        # Ensure local folder exists
        os.makedirs(local_folder, exist_ok=True)
    
    def save_face_image(
        self,
        local_path: str,
        student_id: str,
        filename: str
    ) -> Tuple[str, bool]:
        """
        Save face image to Supabase cloud storage only (no local fallback)
        
        Args:
            local_path: Path to local temporary file
            student_id: Student ID
            filename: Desired filename
            
        Returns:
            Tuple of (public_url, is_cloud_storage)
            
        Raises:
            Exception: If Supabase is not enabled or upload fails
        """
        # Check if Supabase is enabled
        if not self.supabase.is_enabled():
            error_msg = "❌ Supabase storage is not enabled. Please configure SUPABASE_ENABLED=true in .env file"
            logger.error(error_msg)
            raise Exception(error_msg)
        
        remote_path = f"students/{student_id}/{filename}"
        
        # Upload to Supabase (no fallback)
        logger.info(f"📤 Uploading to Supabase cloud storage: {remote_path}")
        success, public_url, error = self.supabase.upload_file(
            local_path=local_path,
            remote_path=remote_path,
            content_type="image/jpeg"
        )
        
        if success and public_url:
            logger.info(f"✅ Successfully saved to Supabase cloud: {public_url}")
            return public_url, True
        else:
            error_msg = f"❌ FAILED to upload to Supabase: {error}"
            logger.error(error_msg)
            raise Exception(error_msg)
    
    def delete_face_image(self, path_or_url: str, student_id: str) -> bool:
        """
        Delete face image from cloud or local storage
        
        Args:
            path_or_url: File path or URL
            student_id: Student ID
            
        Returns:
            Success boolean
        """
        # Check if it's a Supabase URL
        if path_or_url.startswith("http://") or path_or_url.startswith("https://"):
            if self.supabase.is_enabled():
                # Extract remote path from URL
                remote_path = f"students/{student_id}/"
                success, error = self.supabase.delete_file(remote_path)
                return success
            return False
        
        # Local file deletion
        try:
            if os.path.exists(path_or_url):
                os.remove(path_or_url)
                logger.info(f"✅ Deleted local file: {path_or_url}")
                return True
        except Exception as e:
            logger.error(f"❌ Failed to delete local file: {e}")
        
        return False
