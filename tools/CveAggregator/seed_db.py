
import sqlite3
import os

SCHEMA_PATH = r"c:\Users\Akki\Primus SaaS\data\cve-database\schema.sql"
DB_PATH = r"c:\Users\Akki\Primus SaaS\tools\CveAggregator\test-cve.db"

def main():
    if os.path.exists(DB_PATH):
        os.remove(DB_PATH)
        print(f"Removed existing DB at {DB_PATH}")

    # Read Schema
    with open(SCHEMA_PATH, 'r') as f:
        schema_sql = f.read()

    # Create DB and Apply Schema
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    cursor.executescript(schema_sql)
    conn.commit()
    print("Schema applied.")

    # Insert Fake Data
    # 1. Newtonsoft.Json Vulnerability (Before 12.0.2 is bad)
    cursor.execute("""
        INSERT INTO vulnerabilities (cve_id, description, severity, cvss_v3_score)
        VALUES ('CVE-2024-FAKE1', 'Fake vulnerability in Newtonsoft.Json', 'HIGH', 8.5)
    """)
    vuln_id_1 = cursor.lastrowid
    
    cursor.execute("""
        INSERT INTO affected_packages (vulnerability_id, ecosystem, package_name, affected_version_range, patched_version)
        VALUES (?, 'nuget', 'Newtonsoft.Json', '(, 12.0.2)', '12.0.2')
    """, (vuln_id_1,))
    
    # 2. Log4Net Critical (Version 2.0.5 is critical)
    cursor.execute("""
        INSERT INTO vulnerabilities (cve_id, description, severity, cvss_v3_score)
        VALUES ('CVE-2024-FAKE2', 'Critical fake RCE in Log4Net', 'CRITICAL', 9.8)
    """)
    vuln_id_2 = cursor.lastrowid
    
    cursor.execute("""
        INSERT INTO affected_packages (vulnerability_id, ecosystem, package_name, affected_version_range, patched_version)
        VALUES (?, 'nuget', 'log4net', '[2.0.5]', '2.0.8')
    """, (vuln_id_2,))

    conn.commit()
    conn.close()
    print(f"Database seeded successfully at {DB_PATH}")

if __name__ == "__main__":
    main()
