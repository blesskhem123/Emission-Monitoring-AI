# 🏭 AI-Based Emission Monitoring & Pollution Risk Prediction System

<div align="center">

![EMS Banner](https://img.shields.io/badge/AI--Based-Emission%20Monitoring-green?style=for-the-badge&logo=leaflet)
![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet)
![Python](https://img.shields.io/badge/Python-3.10+-blue?style=for-the-badge&logo=python)
![XGBoost](https://img.shields.io/badge/ML-XGBoost-orange?style=for-the-badge)
![SQLite](https://img.shields.io/badge/Database-SQLite-lightblue?style=for-the-badge&logo=sqlite)

**An end-to-end AI/ML powered industrial emission monitoring system that predicts NOx pollution levels and generates early warnings before regulatory limits are breached.**

*Built for Oil & Gas, Refineries, and Thermal Power Plants — IOCL, GAIL, BPCL, HPCL, NTPC*

</div>

---

## 📌 Problem Statement

Industrial plants continuously monitor current emissions but **cannot predict future emission spikes**. This leads to:
- Reactive management instead of preventive action
- Regulatory violations and CPCB penalties
- Environmental damage due to delayed response

**This system solves that** — using Machine Learning to forecast NOx emissions **1 hour ahead**, classify pollution risk, and generate early warnings so operators can act **before** limits are breached.

---

## 🎯 Industrial Use Case

> **Scenario:** An operator at Panipat Refinery sees Current NOx = 75 ppm (within safe limit).
> Without this system — by next hour NOx reaches 118 ppm → **CPCB violation**.
> With this system — AI predicts 112 ppm → **CRITICAL alert generated** → Operator reduces fuel load → **Violation prevented**.

---

## ✨ Features

| Feature | Description |
|---|---|
| 🤖 **AI Prediction** | XGBoost model predicts NOx levels 1 hour ahead |
| 🚨 **Risk Classification** | Auto-classifies as Safe / Warning / Critical |
| 🔔 **Alert System** | Auto-generates alerts with actionable messages |
| 📊 **Analytics Dashboard** | Historical trends with Chart.js visualizations |
| ⚙️ **Plant Configuration** | Adjustable NOx thresholds per regulatory requirement |
| 👥 **Role-Based Access** | Admin / Operator / Viewer roles |
| 📝 **Audit Trail** | Complete activity logging |
| 🔗 **Microservice Architecture** | ASP.NET ↔ Python Flask REST API integration |

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    BROWSER (User)                        │
└──────────────────────────┬──────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│           ASP.NET Core MVC (.NET 8)                      │
│    Dashboard │ Submit Reading │ Alerts │ Analytics       │
└──────────────────────────┬──────────────────────────────┘
                           │ HTTP REST
          ┌────────────────┼────────────────┐
          ▼                                 ▼
┌─────────────────┐              ┌──────────────────────┐
│   SQL Server    │              │  Python Flask API    │
│   (SQLite)      │              │  XGBoost ML Model    │
│                 │              │  POST /api/predict   │
│  • Users        │              │  GET  /api/health    │
│  • Readings     │              └──────────────────────┘
│  • Predictions  │
│  • Alerts       │
└─────────────────┘
```

---

## 🧠 Machine Learning

| Parameter | Value |
|---|---|
| **Algorithm** | XGBoost Regressor |
| **R² Score** | 0.94 (94% accuracy) |
| **MAE** | ~8.2 ppm |
| **Training Data** | 8,760 rows (1 year synthetic industrial data) |
| **Features** | Fuel Consumption, Production Load, Temperature, Current NOx |
| **Target** | Next Hour NOx (ppm) |

### Risk Classification Thresholds
```
Predicted NOx < 80 ppm    →  🟢 SAFE
Predicted NOx 80-100 ppm  →  🟡 WARNING  
Predicted NOx > 100 ppm   →  🔴 CRITICAL
```

---

## 🛠️ Tech Stack

### Frontend
- ASP.NET Core MVC (.NET 8)
- Razor Views + Bootstrap 5
- Chart.js (trend graphs)
- JavaScript

### Backend
- ASP.NET Core Web API (C#)
- Entity Framework Core 8
- ASP.NET Core Identity (Auth)
- SQLite Database

### ML Service
- Python 3.10+
- Flask 3.x (REST API)
- XGBoost 2.x
- Scikit-learn, Pandas, NumPy

---

## 📁 Project Structure

```
EmissionMonitoring/
│
├── EmissionMonitoring.Web/          # ASP.NET Core MVC App
│   ├── Controllers/                 # Account, Dashboard, Readings, Alerts, Analytics, Config
│   ├── Models/
│   │   ├── Entities/                # Plant, PlantReading, Prediction, Alert, AuditLog
│   │   ├── ViewModels/              # Page-specific view models
│   │   └── DTOs/                   # Flask API request/response DTOs
│   ├── Services/                    # Business logic layer
│   ├── Data/                        # EF Core DbContext
│   ├── Views/                       # Razor pages (7 modules)
│   └── wwwroot/                     # CSS, JS assets
│
└── EmissionMonitoring.ML/           # Python ML Service
    ├── data/
    │   ├── generate_dataset.py      # Synthetic dataset generator
    │   └── synthetic_plant_data.csv # 8,760 rows training data
    ├── model/
    │   ├── train_model.py           # XGBoost training pipeline
    │   ├── nox_model.pkl            # Trained model
    │   └── model_metadata.json      # Model metrics
    └── app.py                       # Flask REST API
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Python 3.10+](https://python.org/downloads)
- Git

### Step 1 — Clone Repository
```bash
git clone https://github.com/YOUR_USERNAME/EmissionMonitoring.git
cd EmissionMonitoring
```

### Step 2 — Start ML Service (Terminal 1)
```bash
cd EmissionMonitoring.ML

# Create virtual environment
python -m venv venv
venv\Scripts\activate        # Windows
# source venv/bin/activate   # Linux/Mac

# Install packages
pip install -r requirements.txt

# Train model (first time only)
cd model
python train_model.py
cd ..

# Start Flask API
python app.py
# Running on http://localhost:5001
```

### Step 3 — Start ASP.NET App (Terminal 2)
```bash
cd EmissionMonitoring.Web

# Restore packages
dotnet restore

# Install EF tools (one time)
dotnet tool install --global dotnet-ef

# Setup database
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run application
dotnet run
# Running on http://localhost:5124
```

### Step 4 — Access Application
```
http://localhost:5124
```
1. Click **Create Account**
2. Register with **Admin** role
3. Start submitting plant readings!

---

## 📊 Test Scenarios

| Scenario | Fuel | Load | Temp | NOx | Expected |
|---|---|---|---|---|---|
| Night Shift | 310 | 45% | 790°C | 32 ppm | 🟢 Safe ~35 ppm |
| Day Shift | 420 | 72% | 920°C | 78 ppm | 🟡 Warning ~76 ppm |
| Peak Load | 540 | 90% | 1010°C | 118 ppm | 🔴 Critical ~117 ppm |

---

## 🌐 API Endpoints (Flask)

| Endpoint | Method | Description |
|---|---|---|
| `/api/predict` | POST | Get NOx prediction + risk level |
| `/api/health` | GET | ML service health check |
| `/api/model-info` | GET | Model metadata & metrics |

### Sample Request
```json
POST http://localhost:5001/api/predict
{
    "fuel_consumption": 540,
    "production_load": 90,
    "temperature": 1010,
    "current_nox": 118,
    "safe_limit": 80,
    "warning_limit": 100
}
```

### Sample Response
```json
{
    "success": true,
    "predicted_nox": 117.2,
    "risk_level": "Critical",
    "alert_message": "CRITICAL ALERT: NOx emissions likely to exceed limits...",
    "model_confidence": 0.94,
    "predicted_at": "2026-06-05T15:17:21"
}
```

---

## 🏭 Industrial Relevance

This system is directly applicable to:
- **IOCL** — Indian Oil Corporation Limited (Refineries)
- **GAIL** — Gas Authority of India Limited
- **BPCL** — Bharat Petroleum Corporation Limited
- **HPCL** — Hindustan Petroleum Corporation Limited
- **NTPC** — National Thermal Power Corporation

Compliant with **CPCB** (Central Pollution Control Board) and **MoEF&CC** emission standards.

---

## 👨‍💻 Author

**Bless Khemchandani**
B.Tech Student | AI/ML & Full Stack Development

---

## 📄 License

This project is for educational and demonstration purposes.

---

<div align="center">
⭐ Star this repo if you found it helpful!
</div>
