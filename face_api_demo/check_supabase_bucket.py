"""
Quick diagnostic tool to check what's actually in Supabase bucket
"""

import os
from dotenv import load_dotenv
from supabase import create_client

load_dotenv()

supabase_url = os.getenv('SUPABASE_URL')
supabase_key = os.getenv('SUPABASE_KEY')
bucket = os.getenv('SUPABASE_BUCKET', 'student-photos')

client = create_client(supabase_url, supabase_key)

print(f"\n🔍 Checking Supabase bucket: {bucket}")
print("="*60)

try:
    # List root items
    items = client.storage.from_(bucket).list()
    print(f"\n📦 Root level items: {len(items)}")
    for item in items[:20]:  # Show first 20
        print(f"  - {item.get('name')} (type: {item.get('metadata', {}).get('mimetype', 'folder')})")
    
    # Check if there's a 'students' folder
    print(f"\n\n📁 Checking 'students' folder...")
    try:
        students_items = client.storage.from_(bucket).list('students/')
        print(f"  Found {len(students_items)} items in students/ folder")
        for item in students_items[:10]:
            print(f"    - {item.get('name')}")
    except Exception as e:
        print(f"  No students/ folder or error: {e}")
    
    print("\n" + "="*60)
    
except Exception as e:
    print(f"❌ Error: {e}")
