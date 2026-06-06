"""
============================================================
  AI-Based Emission Monitoring System
  Python Flask ML API
  Endpoints:
    POST /api/predict       — NOx prediction
    GET  /api/health        — Service health check
    GET  /api/model-info    — Model metadata
============================================================
"""

from flask import Flask, request, jsonify
import numpy as np
import joblib
import json
import os
from datetime import datetime

app = Flask(__name__)

# ─────────────────────────────────────────────────────────
# PATHS
# ─────────────────────────────────────────────────────────
BASE_DIR      = os.path.dirname(os.path.abspath(__file__))
MODEL_PATH    = os.path.join(BASE_DIR, 'model', 'nox_model.pkl')
SCALER_PATH   = os.path.join(BASE_DIR, 'model', 'scaler.pkl')
METADATA_PATH = os.path.join(BASE_DIR, 'model', 'model_metadata.json')

# ─────────────────────────────────────────────────────────
# LOAD MODEL AT STARTUP (once, not on every request)
# ─────────────────────────────────────────────────────────
print("🔄 Loading ML model...")
try:
    model    = joblib.load(MODEL_PATH)
    scaler   = joblib.load(SCALER_PATH)
    with open(METADATA_PATH, 'r') as f:
        metadata = json.load(f)
    print("✅ Model loaded successfully")
    print(f"   Model  : {metadata['model_name']}")
    print(f"   R²     : {metadata['metrics']['r2']}")
    print(f"   MAE    : {metadata['metrics']['mae']} ppm")
    MODEL_LOADED = True
except Exception as e:
    print(f"❌ Model load failed: {e}")
    MODEL_LOADED = False


# ─────────────────────────────────────────────────────────
# HELPER — Risk Classification & Alert Message
# ─────────────────────────────────────────────────────────
def classify_risk(predicted_nox, safe_limit=80.0, warning_limit=100.0):
    """
    Classify risk based on predicted NOx and plant-specific limits.
    Limits come from ASP.NET request (plant configuration).
    """
    if predicted_nox < safe_limit:
        risk_level    = 'Safe'
        alert_message = (
            f"NOx emissions are within safe limits. "
            f"Predicted next hour: {predicted_nox:.1f} ppm "
            f"(Safe limit: {safe_limit} ppm). "
            f"Continue normal plant operations."
        )
    elif predicted_nox <= warning_limit:
        risk_level    = 'Warning'
        alert_message = (
            f"NOx emissions are approaching the warning threshold. "
            f"Predicted next hour: {predicted_nox:.1f} ppm "
            f"(Warning limit: {warning_limit} ppm). "
            f"Monitor closely and consider reducing production load."
        )
    else:
        risk_level    = 'Critical'
        alert_message = (
            f"CRITICAL ALERT: NOx emissions are likely to exceed safe limits. "
            f"Predicted next hour: {predicted_nox:.1f} ppm "
            f"(Critical limit: {warning_limit} ppm). "
            f"Immediately reduce fuel consumption or production load to prevent violation."
        )
    return risk_level, alert_message


# ─────────────────────────────────────────────────────────
# HELPER — Input Validation
# ─────────────────────────────────────────────────────────
def validate_inputs(data):
    """Validate that inputs are present and within realistic ranges."""
    required = ['fuel_consumption', 'production_load', 'temperature', 'current_nox']
    errors   = []

    for field in required:
        if field not in data:
            errors.append(f"Missing required field: '{field}'")

    if errors:
        return False, errors

    ranges = {
        'fuel_consumption': (150, 650,  'kg/hr'),
        'production_load' : (20,  100,  '%'),
        'temperature'     : (500, 1200, '°C'),
        'current_nox'     : (0,   200,  'ppm'),
    }

    for field, (lo, hi, unit) in ranges.items():
        val = data.get(field)
        try:
            val = float(val)
            if not (lo <= val <= hi):
                errors.append(
                    f"'{field}' value {val} is out of valid range "
                    f"[{lo}–{hi} {unit}]"
                )
        except (TypeError, ValueError):
            errors.append(f"'{field}' must be a number, got: {val}")

    return (len(errors) == 0), errors


# ═══════════════════════════════════════════════════════════
# ENDPOINT 1 — POST /api/predict
# ═══════════════════════════════════════════════════════════
@app.route('/api/predict', methods=['POST'])
def predict():
    """
    Accepts plant readings, returns NOx prediction + risk level.

    Request JSON:
    {
        "fuel_consumption" : 420.5,
        "production_load"  : 78.0,
        "temperature"      : 840.0,
        "current_nox"      : 75.0,
        "safe_limit"       : 80.0,    (optional — from plant config)
        "warning_limit"    : 100.0    (optional — from plant config)
    }

    Response JSON:
    {
        "success"       : true,
        "predicted_nox" : 112.4,
        "risk_level"    : "Critical",
        "alert_message" : "CRITICAL ALERT: ...",
        "model_confidence" : 0.86,
        "predicted_at"  : "2024-06-02T08:00:00"
    }
    """
    if not MODEL_LOADED:
        return jsonify({
            'success': False,
            'error'  : 'ML model is not loaded. Check server logs.'
        }), 503

    # ── Parse request body ──
    try:
        data = request.get_json(force=True)
    except Exception:
        return jsonify({
            'success': False,
            'error'  : 'Invalid JSON in request body.'
        }), 400

    if not data:
        return jsonify({
            'success': False,
            'error'  : 'Empty request body.'
        }), 400

    # ── Validate ──
    is_valid, errors = validate_inputs(data)
    if not is_valid:
        return jsonify({
            'success': False,
            'error'  : 'Validation failed',
            'details': errors
        }), 422

    # ── Extract inputs ──
    fuel_consumption = float(data['fuel_consumption'])
    production_load  = float(data['production_load'])
    temperature      = float(data['temperature'])
    current_nox      = float(data['current_nox'])

    # Plant-specific limits (with defaults)
    safe_limit    = float(data.get('safe_limit',    80.0))
    warning_limit = float(data.get('warning_limit', 100.0))

    # ── Run prediction ──
    try:
        input_array  = np.array([[fuel_consumption, production_load,
                           temperature, current_nox]])
# Use model directly without scaling — retrain fixes this properly
        input_scaled = input_array
        predicted_nox = float(model.predict(input_scaled)[0])
        print(f"DEBUG — Input: {input_array}, Scaled: {input_scaled}, Predicted: {predicted_nox}")
        predicted_nox = round(max(0, predicted_nox), 2)   # no negative ppm
    except Exception as e:
        return jsonify({
            'success': False,
            'error'  : f'Prediction failed: {str(e)}'
        }), 500

    # ── Classify risk ──
    risk_level, alert_message = classify_risk(
        predicted_nox, safe_limit, warning_limit
    )

    # ── Response ──
    response = {
        'success'          : True,
        'predicted_nox'    : predicted_nox,
        'risk_level'       : risk_level,
        'alert_message'    : alert_message,
        'model_confidence' : metadata['metrics']['r2'],
        'predicted_at'     : datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S'),
        'inputs_received'  : {
            'fuel_consumption': fuel_consumption,
            'production_load' : production_load,
            'temperature'     : temperature,
            'current_nox'     : current_nox
        },
        'thresholds_used'  : {
            'safe_limit'    : safe_limit,
            'warning_limit' : warning_limit
        }
    }

    return jsonify(response), 200


# ═══════════════════════════════════════════════════════════
# ENDPOINT 2 — GET /api/health
# ═══════════════════════════════════════════════════════════
@app.route('/api/health', methods=['GET'])
def health():
    """
    Health check — ASP.NET calls this to verify ML service is alive.
    """
    return jsonify({
        'status'      : 'healthy' if MODEL_LOADED else 'degraded',
        'model_loaded': MODEL_LOADED,
        'service'     : 'NOx Emission Prediction API',
        'version'     : metadata.get('version', '1.0.0') if MODEL_LOADED else 'N/A',
        'timestamp'   : datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%S')
    }), 200 if MODEL_LOADED else 503


# ═══════════════════════════════════════════════════════════
# ENDPOINT 3 — GET /api/model-info
# ═══════════════════════════════════════════════════════════
@app.route('/api/model-info', methods=['GET'])
def model_info():
    """
    Returns model metadata — useful for dashboard display.
    """
    if not MODEL_LOADED:
        return jsonify({'success': False, 'error': 'Model not loaded'}), 503

    return jsonify({
        'success' : True,
        'metadata': metadata
    }), 200


# ─────────────────────────────────────────────────────────
# RUN SERVER
# ─────────────────────────────────────────────────────────
if __name__ == '__main__':
    print("\n" + "═"*50)
    print("  NOx Emission Prediction — Flask API")
    print("  Running on http://localhost:5001")
    print("  Endpoints:")
    print("    POST /api/predict")
    print("    GET  /api/health")
    print("    GET  /api/model-info")
    print("═"*50 + "\n")
    app.run(host='0.0.0.0', port=5001, debug=False)
