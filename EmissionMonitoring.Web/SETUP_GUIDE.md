# Emission Monitoring System — Setup Guide

## Prerequisites
- Visual Studio 2022 (or VS Code with C# extension)
- .NET 8 SDK — https://dotnet.microsoft.com/download/dotnet/8
- SQL Server 2019/2022 (or SQL Server Express — free)
- Python 3.10+ (for Flask ML API)

---

## Step 1 — Run Python ML Service first

```bash
cd EmissionMonitoring.ML

# Install Python packages
pip install -r requirements.txt

# Generate dataset (if not already done)
cd data
python generate_dataset.py

# Train model (if not already done)
cd ../model
python train_model.py

# Start Flask API
cd ..
python app.py
# Flask runs on: http://localhost:5001
```

---

## Step 2 — Configure SQL Server connection

Open `appsettings.json` and update the connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=EmissionMonitoringDB;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

Common server names:
- Local SQL Server Express: `(localdb)\\mssqllocaldb`
- SQL Server installed locally: `.` or `localhost`

---

## Step 3 — Run ASP.NET Application

```bash
cd EmissionMonitoring.Web

# Restore NuGet packages
dotnet restore

# Create database + run migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run the app
dotnet run
```

Open browser: `https://localhost:5001` (or the port shown in terminal)

---

## Step 4 — First Time Login

1. Click **Create Account** on the Login page
2. Register as **Admin** (first user)
3. Start submitting readings!

---

## Project URLs
| Page           | URL                          |
|----------------|------------------------------|
| Login          | /Account/Login               |
| Register       | /Account/Register            |
| Dashboard      | /Dashboard                   |
| Submit Reading | /Readings/Submit             |
| Reading History| /Readings                    |
| Alert Center   | /Alerts                      |
| Analytics      | /Analytics                   |
| Plant Config   | /Config  (Admin only)        |

---

## Flask API Endpoints
| Endpoint         | Method | Purpose              |
|------------------|--------|----------------------|
| /api/predict     | POST   | Get NOx prediction   |
| /api/health      | GET    | Service health check |
| /api/model-info  | GET    | Model metadata       |

---

## Troubleshooting

**"ML Service Offline" badge on Dashboard?**
→ Start Flask API: `python app.py` in EmissionMonitoring.ML folder

**Database connection error?**
→ Check SQL Server is running + correct server name in appsettings.json

**NuGet restore fails?**
→ Check internet connection, then run `dotnet restore` again
