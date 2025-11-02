# 📘 Face Recognition Attendance System - User Guide

## Welcome! 👋

This guide will help you use the Face Recognition Attendance System effectively. No technical knowledge required!

---
1. Activate Visual Environment in Python Server: .\venv\Scripts\Activate.ps1

## 📋 Table of Contents

1. [Getting Started](#getting-started)
2. [Managing Students](#managing-students)
3. [Managing Classes](#managing-classes)
4. [Taking Attendance](#taking-attendance)
5. [Viewing Reports](#viewing-reports)
6. [Tips & Best Practices](#tips--best-practices)
7. [Troubleshooting](#troubleshooting)

---

## 🚀 Getting Started

### Logging In

1. Open your web browser
2. Navigate to the system URL (provided by your administrator)
3. Enter your username and password
4. Click **"Login"**

### Dashboard Overview

After logging in, you'll see the main dashboard with:
- **Total Students** - Number of students in the system
- **Active Classes** - Currently running classes
- **Today's Sessions** - Attendance sessions for today
- **Quick Actions** - Shortcuts to common tasks

---

## 👥 Managing Students

### Adding a New Student

#### Step 1: Navigate to Students Page
- Click **"Students"** in the navigation menu
- Click the **"Add Student"** button (+ icon)

#### Step 2: Fill in Student Information
- **Student Number** (Required) - Unique identifier for the student
- **First Name** (Required)
- **Last Name** (Required)
- **Email** (Required) - Student's email address

#### Step 3: Add Student Photo

You have **two options** to add a photo:

##### Option A: Upload a Photo File
1. Click the **"Upload"** tab
2. Click **"Choose File"**
3. Select a photo from your computer
4. See the preview of your selected photo

##### Option B: Use Webcam (Recommended)
1. Click the **"Webcam"** tab
2. Click **"Start Camera"**
3. Allow camera access when prompted by your browser
4. Position the student's face in the frame
5. Click **"Capture Photo"**
6. Review the captured photo
7. If not satisfied, click **"Retake"**

#### Step 4: Submit
- Click **"Create Student"**
- Wait for confirmation: "Student created successfully!"
- The new student appears in the student list

### Photo Requirements ⚠️

For best results, ensure:
- ✅ Clear, well-lit face photo
- ✅ Only ONE face in the photo
- ✅ Face looking directly at camera
- ✅ No sunglasses or face coverings
- ✅ Photo format: JPG, JPEG, or PNG
- ✅ File size: Less than 5MB

❌ Avoid:
- Multiple people in the photo
- Blurry or low-quality images
- Extreme angles
- Poor lighting

### Editing Student Information

1. Find the student in the list (use search box if needed)
2. Click the **"Edit"** button (pencil icon)
3. Update the information
4. To change photo, upload a new one
5. Click **"Update Student"**

### Deleting a Student

1. Find the student in the list
2. Click the **"Delete"** button (trash icon)
3. Confirm the deletion
4. Student is removed from the system

⚠️ **Warning:** Deleting a student removes all their attendance records!

---

## 🏫 Managing Classes

### Creating a New Class

1. Click **"Classes"** in the navigation menu
2. Click **"Add Class"** button
3. Fill in:
   - **Class Code** - Unique identifier (e.g., CS101)
   - **Class Name** - Full name (e.g., Introduction to Computer Science)
   - **Schedule** - Days and time
   - **Instructor** - Teacher's name
4. Click **"Create Class"**

### Enrolling Students in a Class

#### Method 1: From Class Page
1. Open the class details
2. Click **"Enroll Students"**
3. Select students from the list (check boxes)
4. Click **"Add Selected Students"**

#### Method 2: From Student Page
1. Open student details
2. Click **"Enroll in Class"**
3. Select the class
4. Click **"Enroll"**

### Setting Up Face Recognition for a Class

**This is IMPORTANT before taking attendance!**

1. Open the class details
2. Click **"Setup Face Database"** button
3. System will process all enrolled students' faces
4. Wait for "Setup complete" message
5. Now you can take attendance for this class

⏱️ **Note:** This may take a few minutes depending on the number of students.

---

## ✅ Taking Attendance

### Creating an Attendance Session

1. Click **"Take Attendance"** in the navigation menu
2. Click **"New Session"**
3. Select:
   - **Class** - Which class is this for
   - **Date** - Today's date (default)
   - **Session Type** - Lecture, Lab, Tutorial, etc.
4. Click **"Create Session"**
5. Session status: **"In Progress"**

### Method 1: Automatic Face Recognition (Recommended)

#### Using Group Photo Upload:
1. In the active session, click **"Scan Attendance"**
2. Click **"Upload Photo"**
3. Select a photo containing students' faces
4. Click **"Recognize"**
5. Wait for processing (usually 5-10 seconds)
6. System shows recognized students with confidence scores
7. Review and confirm the attendance

#### Using Camera:
1. Click **"Use Camera"** instead
2. Position camera to capture students
3. Click **"Capture"**
4. System automatically recognizes faces
5. Review and confirm

### Method 2: Manual Attendance

1. In the active session, find the student list
2. For each student, click the status button:
   - **Present** - Student is here
   - **Absent** - Student is not here
   - **Late** - Student arrived late
   - **Excused** - Excused absence
3. Status updates immediately

### Tips for Best Face Recognition Results

✅ **DO:**
- Use good lighting
- Capture clear, frontal faces
- Keep camera steady
- Include 2-5 students per photo for best accuracy
- Take multiple photos if needed

❌ **DON'T:**
- Use blurry or dark photos
- Capture from extreme angles
- Include too many people (max 10-15)
- Rush the process

### Ending a Session

1. After marking attendance, click **"End Session"**
2. Session status changes to **"Completed"**
3. No further changes can be made
4. Attendance is recorded permanently

---

## 📊 Viewing Reports

### Attendance Records

1. Click **"Reports"** in the navigation menu
2. Select date range
3. Select class (optional - leave blank for all)
4. Click **"Generate Report"**

View:
- Total sessions held
- Average attendance rate
- Individual student attendance
- Absent students list

### Exporting Data

1. In any report view, click **"Export"**
2. Choose format:
   - **Excel** - For spreadsheets
   - **PDF** - For printing
   - **CSV** - For data analysis
3. File downloads automatically
4. Open with appropriate software

### Student Attendance History

1. Go to **"Students"** page
2. Click on a student's name
3. View their attendance history:
   - Total sessions
   - Present count
   - Absent count
   - Attendance percentage
   - Date-wise breakdown

---

## 💡 Tips & Best Practices

### For Student Photos

- 📸 Take photos in a **well-lit area**
- 🧑 Capture **one student at a time**
- 👀 Ensure face is **clearly visible**
- 🔄 Retake if photo quality is poor
- 🌅 Morning light works best for webcam captures

### For Attendance Scanning

- 👥 Group students in **small batches** (3-5 per photo)
- 📏 Keep **consistent distance** from camera
- ⏰ Allow **5-10 seconds** for recognition
- ✔️ **Double-check** recognized students
- 🔁 Use **manual override** if system misses someone

### For Accuracy

- 🔄 **Update photos** if student's appearance changes significantly
- 📚 **Setup class database** before first attendance session
- 🧹 **Remove** inactive students from class enrollments
- 📊 **Review** attendance records regularly

---

## 🔧 Troubleshooting

### "No face detected in image"

**Problem:** System cannot find a face in the uploaded photo.

**Solutions:**
- ✅ Ensure face is clearly visible
- ✅ Use better lighting
- ✅ Remove sunglasses or hats
- ✅ Try a different photo
- ✅ Face camera directly

---

### "Multiple faces detected"

**Problem:** More than one face found when adding a student.

**Solutions:**
- ✅ Take a photo with only the student
- ✅ Crop the photo to show one face
- ✅ Use the webcam option (easier to control)

---

### "No students recognized"

**Problem:** Face recognition found faces but couldn't identify them.

**Solutions:**
- ✅ Verify class face database is setup
- ✅ Check if students are enrolled in this class
- ✅ Ensure photo quality is good
- ✅ Try with better lighting
- ✅ Verify students' photos in system are current
- ✅ Use manual attendance as backup

---

### "Student not in the list"

**Problem:** Student is missing from the class.

**Solutions:**
- ✅ Check if student is enrolled in the class
- ✅ Verify student exists in the system
- ✅ Setup class database again
- ✅ Contact administrator if issue persists

---

### "Failed to create student"

**Problem:** Error when adding new student.

**Solutions:**
- ✅ Check internet connection
- ✅ Verify student number is unique
- ✅ Ensure all required fields are filled
- ✅ Check photo file size (must be under 5MB)
- ✅ Try a different photo format (JPG recommended)
- ✅ Contact administrator if problem continues

---

### "Camera not working"

**Problem:** Webcam doesn't start.

**Solutions:**
- ✅ Allow camera access when browser prompts
- ✅ Check camera is connected and working
- ✅ Close other apps using the camera
- ✅ Try a different browser (Chrome recommended)
- ✅ Restart your computer
- ✅ Use photo upload method instead

---

### Photo Upload Issues

**Problem:** Cannot upload photos to the system.

**Solutions:**
- ✅ Check file format (JPG, JPEG, PNG only)
- ✅ Verify file size is under 5MB
- ✅ Ensure stable internet connection
- ✅ Try compressing the image
- ✅ Use a different browser
- ✅ Contact administrator for help

---

## 🎯 Common Workflows

### Quick Start: Adding First Student

1. Click **"Students"** → **"Add Student"**
2. Enter: Student Number, Name, Email
3. Click **"Webcam"** tab → **"Start Camera"**
4. Position face → **"Capture Photo"**
5. **"Create Student"** → Done! ✅

### Quick Start: Taking First Attendance

1. **Setup Phase** (One-time per class):
   - Create class
   - Enroll students
   - Setup face database

2. **Daily Use**:
   - **"Take Attendance"** → **"New Session"**
   - Select class and date
   - **"Scan Attendance"** → Upload group photo
   - Review recognized students
   - **"End Session"** → Done! ✅

### Weekly Routine

**Monday:**
- Review last week's attendance
- Export reports for records
- Follow up on absent students

**Daily:**
- Create attendance session
- Scan or manually mark attendance
- End session when complete

**Friday:**
- Generate weekly attendance report
- Update any missed records
- Plan for next week

---

## 📞 Need Help?

### Before Contacting Support

- ✅ Check this user guide
- ✅ Try the troubleshooting steps
- ✅ Verify your internet connection
- ✅ Restart your browser
- ✅ Clear browser cache

### Contact Information

**Technical Support:**
- 📧 Email: support@yourschool.edu
- 📞 Phone: (555) 123-4567
- 🕐 Hours: Monday-Friday, 8AM-5PM

**When contacting support, please provide:**
- Your name and role
- Class/student affected
- What you were trying to do
- Error message (if any)
- Screenshot (if possible)

---

## 📝 Quick Reference Card

### Student Management
- ➕ Add Student: Students → Add Student → Fill form → Submit
- ✏️ Edit Student: Students → Find → Edit → Update
- 🗑️ Delete: Students → Find → Delete → Confirm

### Attendance
- 🆕 New Session: Take Attendance → New Session → Select class
- 🤖 Face Scan: Scan Attendance → Upload/Camera → Recognize
- ✋ Manual: Click student status (Present/Absent/Late/Excused)
- ✅ End: End Session button

### Reports
- 📊 View: Reports → Select dates → Generate
- 💾 Export: Export → Choose format (Excel/PDF/CSV)

---

## 🎓 Best Practices Summary

1. **Always** setup class database before first attendance
2. **Take clear** photos with good lighting
3. **Review** recognized students before confirming
4. **Update** student photos when appearance changes
5. **Export** attendance reports regularly
6. **End sessions** promptly after attendance
7. **Use webcam** for best photo quality
8. **Keep** browser updated for best performance

---

## ✅ System Requirements

### For Best Experience

**Browser:** (Latest version recommended)
- ✅ Google Chrome (Recommended)
- ✅ Microsoft Edge
- ✅ Firefox
- ✅ Safari

**Device:**
- 💻 Computer/Laptop with webcam (for camera capture)
- 📱 Tablet/Phone supported for viewing
- 🌐 Stable internet connection
- 📸 Camera/Webcam (optional, for photo capture)

---

## 🔒 Privacy & Security

- 🔐 Your password is encrypted and secure
- 📸 Student photos are stored securely in the cloud
- 🛡️ Only authorized users can access the system
- 🗑️ Deleted data is permanently removed
- 📊 Reports contain only necessary information
- 🔏 All data transfers are encrypted

---

## 📱 Mobile Usage Tips

While the system works on mobile devices:

✅ **Good for:**
- Viewing attendance records
- Checking reports
- Manual attendance marking
- Student list viewing

⚠️ **Better on Desktop:**
- Adding new students with photos
- Face recognition scanning
- Class database setup
- Bulk operations

💡 **Tip:** Use mobile for viewing, desktop for data entry and scanning.

---

## 🎉 Congratulations!

You're now ready to use the Face Recognition Attendance System effectively!

Remember:
- 📚 Refer back to this guide anytime
- 💬 Ask for help when needed
- 🎯 Practice makes perfect
- ⭐ Clear photos = Better recognition

**Happy teaching! 👨‍🏫👩‍🏫**

---

**Document Version:** 1.0  
**Last Updated:** November 2, 2025  
**For System Version:** 1.0

---

## 📋 Appendix: Glossary

**Term** | **Meaning**
---------|------------
**Session** | A single attendance recording event for a class
**Face Database** | Collection of student faces used for recognition
**Recognition** | System identifying students from photos
**Confidence Score** | How certain the system is about a match (0-100%)
**Manual Override** | Manually marking attendance instead of using face recognition
**Enrolled Students** | Students registered in a specific class
**Active Session** | An ongoing attendance session (In Progress status)

---

**End of User Guide**
