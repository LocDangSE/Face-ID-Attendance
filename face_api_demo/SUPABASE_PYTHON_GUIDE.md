# Python Flask Server - Supabase Integration Guide

## ✅ What's New

The Python Flask face recognition server now supports **Supabase cloud storage** for face images with automatic fallback to local storage.

---

## 📦 Installation

### 1. Install Dependencies

```bash
cd D:\Capstone\Test\face_api_demo
pip install supabase==2.3.0 python-dotenv==1.0.0
```

Or install all requirements:

```bash
pip install -r requirements.txt
```

---

## 🔧 Configuration

### 1. Create Environment File

Create a `.env` file in the `face_api_demo` folder:

```bash
# Copy the example
copy .env.example .env
```

### 2. Edit `.env` File

Open `.env` and configure:

```env
# Supabase Configuration
SUPABASE_URL=https://your-project-id.supabase.co
SUPABASE_KEY=your-anon-public-key-here
SUPABASE_BUCKET=student-photos
SUPABASE_ENABLED=true

# Flask Configuration
FLASK_DEBUG=True
FLASK_PORT=5000
```

**Important**: Use the **same** Supabase credentials as your .NET backend!

---

## 🚀 Running the Server

### With Supabase Enabled

```bash
cd D:\Capstone\Test\face_api_demo
python app.py
```

You should see:

```
============================================================
Face Recognition API Service Starting...
Model: Facenet512
Distance Metric: cosine
Detector: opencv
Confidence Threshold: 0.6
------------------------------------------------------------
Storage Configuration:
  ✅ Supabase Cloud Storage: ENABLED
     Bucket: student-photos
  💾 Local Storage: face_database
============================================================
```

### With Supabase Disabled

Set in `.env`:
```env
SUPABASE_ENABLED=false
```

Server will use local storage only.

---

## 🎯 How It Works

### Hybrid Storage System

```
Image Upload
    ↓
Is Supabase Enabled?
    ├─ YES → Try upload to Supabase
    │         ↓
    │         Success? → Return public URL
    │         Failed? → Fall back to local storage
    │
    └─ NO → Save to local storage
    
DeepFace Recognition
    ↓
Always uses LOCAL copies
(DeepFace.find() requires local files)
```

**Key Point**: For face recognition, DeepFace needs local file access. So the system:
1. Saves images to Supabase for **public access** (URLs for frontend)
2. Keeps local copies for **DeepFace recognition**

---

## 📡 API Endpoints Updated

### 1. Health Check - Now Shows Storage Status

**GET** `/health`

Response:
```json
{
  "status": "healthy",
  "service": "Face Recognition API",
  "model": "Facenet512",
  "storage": {
    "supabase": {
      "enabled": true,
      "bucket": "student-photos"
    },
    "local": {
      "enabled": true,
      "path": "face_database"
    }
  }
}
```

### 2. Register Face - Returns Storage Info

**POST** `/api/face/register`

Response now includes:
```json
{
  "success": true,
  "message": "Face registered successfully",
  "studentId": "abc123",
  "facePath": "https://project.supabase.co/storage/v1/object/public/student-photos/students/abc123/...",
  "storageType": "cloud (Supabase)",
  "isCloudStorage": true,
  "confidence": 0.9876
}
```

### 3. Setup Class Database - Saves to Both

**POST** `/api/face/class/setup`

Now saves images:
- To Supabase (public URLs)
- To local storage (for DeepFace)

Response includes both paths:
```json
{
  "students": [
    {
      "studentId": "abc123",
      "success": true,
      "facePath": "https://...",  // Supabase URL
      "localPath": "face_database/class_id/abc123/...",  // Local path
      "isCloudStorage": true
    }
  ]
}
```

---

## 🧪 Testing

### 1. Test Health Check

```bash
curl http://localhost:5000/health
```

Should show Supabase enabled.

### 2. Test Face Registration

```bash
curl -X POST http://localhost:5000/api/face/register \
  -F "image=@student_photo.jpg" \
  -F "studentId=test123"
```

Check response for:
- `facePath` should be Supabase URL (if enabled)
- `isCloudStorage` should be `true`

### 3. Verify in Supabase Dashboard

Go to Supabase → Storage → student-photos → students/

You should see uploaded images.

---

## 📂 File Structure

```
face_api_demo/
├── app.py                    # Main Flask app (updated)
├── supabase_storage.py      # NEW: Supabase storage service
├── requirements.txt         # Updated dependencies
├── .env                     # Your configuration (gitignored)
├── .env.example             # Template for configuration
├── face_database/           # Local storage (still used)
│   ├── student_id/          # Student face images
│   └── class_id/            # Class databases
└── temp/                    # Temporary uploads
```

---

## 🔍 Troubleshooting

### Issue: "Supabase storage is not enabled"

**Solution**: 
1. Check `.env` file exists
2. Verify `SUPABASE_ENABLED=true`
3. Ensure URL and KEY are correct
4. Restart Flask server

### Issue: Images not appearing in Supabase

**Solution**:
1. Verify bucket name matches: `student-photos`
2. Check bucket is public
3. Verify storage policies allow INSERT
4. Check Flask console for upload errors

### Issue: Face recognition not working

**Solution**:
- DeepFace requires LOCAL files
- Check `face_database/` folder has student images
- Hybrid storage automatically creates local copies

### Issue: Import errors when running

**Solution**:
```bash
pip install supabase python-dotenv
```

---

## 🔒 Security Notes

1. **Never commit `.env`** - Already in `.gitignore`
2. **Use anon key** - Not service_role key
3. **Public bucket** - Images must be publicly accessible
4. **Storage policies** - Set up proper RLS policies

---

## ⚙️ Configuration Options

### Disable Supabase

In `.env`:
```env
SUPABASE_ENABLED=false
```

Server will work normally with local storage only.

### Change Bucket Name

In `.env`:
```env
SUPABASE_BUCKET=my-custom-bucket
```

Make sure bucket exists in Supabase!

---

## 🔄 Migration from Local to Supabase

If you have existing local images:

1. Enable Supabase
2. Existing local images remain functional
3. New uploads go to Supabase
4. Optional: Manually upload old images to Supabase

---

## 📊 Storage Comparison

| Feature | Local Storage | Supabase Storage |
|---------|--------------|------------------|
| Setup | ✅ None needed | Requires config |
| Cost | Server disk | Free tier: 1GB |
| Access | Server only | Global public URLs |
| Backup | Manual | Automatic |
| Scalability | Limited | Unlimited |

---

## ✅ Checklist

- [ ] Install dependencies: `pip install supabase python-dotenv`
- [ ] Create `.env` file from `.env.example`
- [ ] Add Supabase URL and KEY
- [ ] Set `SUPABASE_ENABLED=true`
- [ ] Verify bucket `student-photos` exists in Supabase
- [ ] Restart Flask server
- [ ] Test with health check endpoint
- [ ] Upload test student photo
- [ ] Verify image in Supabase dashboard

---

## 🚀 Quick Start Commands

```bash
# Install dependencies
cd D:\Capstone\Test\face_api_demo
pip install -r requirements.txt

# Configure (edit with your credentials)
notepad .env

# Run server
python app.py

# Test in another terminal
curl http://localhost:5000/health
```

---

## 📚 Related Documentation

- **Backend (C#)**: `SUPABASE_INTEGRATION_SUMMARY.md`
- **Quick Start**: `SUPABASE_QUICK_START.md`
- **Setup Guide**: `SUPABASE_SETUP_GUIDE.md`

---

**Status**: ✅ Python Flask Server Ready for Supabase!

**Implementation Date**: November 2, 2025
