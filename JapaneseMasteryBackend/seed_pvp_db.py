import sqlite3
import os

DB_PATH = r"d:\Game\SWD\JapaneseMasteryBackend\test.db" # Or wherever your DB is located
TXT_PATH = r"d:\Game\SWD\JapaneseMasteryBackend\pvp_paragraphs.txt"

def seed_database():
    print(f"Connecting to DB at {DB_PATH}")
    
    # Create the db file if it doesn't exist
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    # Create table for PvP paragraphs
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS pvp_paragraphs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            content TEXT NOT NULL
        )
    """)
    
    print("Reading paragraphs...")
    with open(TXT_PATH, "r", encoding="utf-8") as f:
        paragraphs = [line.strip() for line in f if line.strip()]
        
    print(f"Inserting {len(paragraphs)} paragraphs into DB...")
    
    # Clear existing to avoid duplicates if re-run
    cursor.execute("DELETE FROM pvp_paragraphs")
    
    for p in paragraphs:
        cursor.execute("INSERT INTO pvp_paragraphs (content) VALUES (?)", (p,))
        
    conn.commit()
    conn.close()
    print("Database seeded successfully.")

if __name__ == "__main__":
    seed_database()
