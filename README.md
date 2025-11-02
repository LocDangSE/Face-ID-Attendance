#  Face Recognition Attendance System

A comprehensive attendance management system using facial recognition technology with .NET backend, React frontend, and Python Flask face recognition service.

---

##  Features

-  Face Recognition using DeepFace (Facenet512)
-  Real-time Attendance via Webcam
-  Student & Class Management
-  Attendance Reports & Excel Export
-  Dashboard with Statistics
-  Cloud Storage (Supabase)

---

##  Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or Developer Edition)
- [Node.js](https://nodejs.org/) v18 or later
- [Python 3.11](https://www.python.org/downloads/) or later

---

##  Quick Setup

### 1. Clone Repository
```bash
git clone <your-repository-url>
cd Test
```

### 2. Database Setup
```bash
# Open SQL Server Management Studio (SSMS)
# Run the SQL script: AzureAIQuery.sql
```

### 3. Configuration Files
**Copy and paste the environment files provided:**
- `FaceIdBackend/FaceIdBackend/appsettings.json`
- `face_api_demo/.env`

### 4. Install Dependencies

**Python Flask API:**
```bash
cd face_api_demo
python -m venv venv
.\venv\Scripts\Activate.ps1    # Windows PowerShell
pip install -r requirements.txt
```

**React Frontend:**
```bash
cd face-attendance-ui
npm install
```

**.NET Backend:**
```bash
cd FaceIdBackend
dotnet restore
dotnet build
```

### 5. Run All Services

**Terminal 1 - Python Flask:**
```bash
cd face_api_demo
.\venv\Scripts\Activate.ps1
python app.py
```
 Running on http://localhost:5000

**Terminal 2 - .NET Backend:**
```bash
cd FaceIdBackend/FaceIdBackend
dotnet run
```
 Running on http://localhost:5137

**Terminal 3 - React Frontend:**
```bash
cd face-attendance-ui
npm run dev
```
 Running on http://localhost:5173

---

##  Basic Usage

1. **Add Students**  Upload student photos
2. **Create Class**  Set up your class
3. **Enroll Students**  Add students to class
4. **Setup Face Database**  Sync photos for recognition
5. **Create Session**  Start attendance session
6. **Take Attendance**  Capture faces via webcam
7. **View Reports**  Check attendance statistics

---

##  Common Issues

| Issue | Solution |
|-------|----------|
| No faces detected | Ensure good lighting, face front-facing, no obstructions |
| No students recognized | Run "Setup Face Database" from Classes page |
| Connection refused | Ensure all 3 servers are running |
| Database errors | Verify SQL Server is running, check connection string |
| Camera not working | Allow camera permissions in browser (Chrome recommended) |

---

##  API Documentation

Visit Swagger UI after starting .NET backend:
- **Swagger**: `http://localhost:5137/swagger`

**Key Endpoints:**
- `POST /api/attendance/recognize` - Recognize face and mark attendance
- `GET /api/students` - Get all students
- `GET /api/classes` - Get all classes

---

##  Tech Stack

**Backend:** .NET 8, Entity Framework Core, SQL Server  
**Frontend:** React 18, Vite, Tailwind CSS  
**Face Recognition:** Python Flask, DeepFace, TensorFlow, Facenet512

---

##  System Requirements

**Minimum:** Dual-core CPU, 8GB RAM, 5GB Storage  
**Recommended:** Quad-core CPU, 16GB RAM, 10GB Storage

---

**Version:** 1.0 | **Last Updated:** November 2, 2025
