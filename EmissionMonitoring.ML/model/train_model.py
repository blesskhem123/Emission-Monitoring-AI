"""
============================================================
  AI-Based Emission Monitoring System
  NOx Prediction Model — Training Script
  Algorithm : XGBoost Regressor
  Author    : EmissionMonitoring Project
============================================================
"""

import pandas as pd
import numpy as np
import matplotlib
matplotlib.use('Agg')   # Non-interactive backend for saving plots
import matplotlib.pyplot as plt
import matplotlib.gridspec as gridspec
import seaborn as sns
import joblib
import json
import os
import warnings
warnings.filterwarnings('ignore')

from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.preprocessing   import StandardScaler
from sklearn.metrics         import mean_absolute_error, mean_squared_error, r2_score
from xgboost                 import XGBRegressor

# ─────────────────────────────────────────────────────────
# PATHS
# ─────────────────────────────────────────────────────────
BASE_DIR      = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA_PATH     = os.path.join(BASE_DIR, 'data',  'synthetic_plant_data.csv')
MODEL_DIR     = os.path.join(BASE_DIR, 'model')
PLOTS_DIR     = os.path.join(BASE_DIR, 'notebooks', 'eda_plots')
MODEL_PATH    = os.path.join(MODEL_DIR, 'nox_model.pkl')
SCALER_PATH   = os.path.join(MODEL_DIR, 'scaler.pkl')
METADATA_PATH = os.path.join(MODEL_DIR, 'model_metadata.json')

os.makedirs(PLOTS_DIR, exist_ok=True)
os.makedirs(MODEL_DIR, exist_ok=True)

FEATURE_COLS = ['FuelConsumption', 'ProductionLoad', 'Temperature', 'CurrentNox']
TARGET_COL   = 'NextHourNox'

# NOx thresholds (ppm)
SAFE_LIMIT     = 80.0
WARNING_LIMIT  = 100.0
CRITICAL_LIMIT = 120.0


# ═══════════════════════════════════════════════════════════
# SECTION 1 — LOAD DATA
# ═══════════════════════════════════════════════════════════
def load_data():
    print("\n" + "═"*55)
    print("  STEP 1 — LOADING DATASET")
    print("═"*55)

    df = pd.read_csv(DATA_PATH, parse_dates=['Timestamp'])
    print(f"  ✅ Rows    : {len(df):,}")
    print(f"  ✅ Columns : {df.shape[1]}")
    print(f"\n  First 3 rows:")
    print(df[FEATURE_COLS + [TARGET_COL, 'RiskLabel']].head(3).to_string(index=False))

    # Validation checks
    assert df.isnull().sum().sum() == 0,        "❌ Missing values found!"
    assert df['FuelConsumption'].between(150, 620).all(), "❌ FuelConsumption out of range"
    assert df['ProductionLoad'].between(30, 101).all(),   "❌ ProductionLoad out of range"
    assert df['Temperature'].between(550, 1150).all(),    "❌ Temperature out of range"
    assert df['CurrentNox'].between(15, 160).all(),       "❌ CurrentNox out of range"
    assert df['NextHourNox'].between(15, 165).all(),      "❌ NextHourNox out of range"
    print("\n  ✅ All validation checks passed")

    print(f"\n  Risk Distribution:")
    dist = df['RiskLabel'].value_counts()
    pct  = df['RiskLabel'].value_counts(normalize=True).mul(100).round(1)
    for label in ['Safe', 'Warning', 'Critical']:
        if label in dist:
            print(f"    {label:10s}: {dist[label]:5,}  ({pct[label]}%)")

    return df


# ═══════════════════════════════════════════════════════════
# SECTION 2 — EDA PLOTS
# ═══════════════════════════════════════════════════════════
def run_eda(df):
    print("\n" + "═"*55)
    print("  STEP 2 — EXPLORATORY DATA ANALYSIS")
    print("═"*55)

    sns.set_style("whitegrid")
    palette = {'Safe': '#2ecc71', 'Warning': '#f39c12', 'Critical': '#e74c3c'}

    # ── Plot 1: Risk Distribution ──
    fig, ax = plt.subplots(figsize=(7, 4))
    counts = df['RiskLabel'].value_counts()
    bars   = ax.bar(counts.index,
                    counts.values,
                    color=[palette.get(x, 'gray') for x in counts.index],
                    edgecolor='white', linewidth=1.5, width=0.5)
    for bar, val in zip(bars, counts.values):
        ax.text(bar.get_x() + bar.get_width()/2, bar.get_height() + 30,
                f'{val:,}\n({val/len(df)*100:.1f}%)',
                ha='center', va='bottom', fontsize=10, fontweight='bold')
    ax.set_title('Risk Level Distribution', fontsize=14, fontweight='bold', pad=15)
    ax.set_xlabel('Risk Level', fontsize=11)
    ax.set_ylabel('Number of Readings', fontsize=11)
    ax.set_ylim(0, counts.max() * 1.2)
    plt.tight_layout()
    plt.savefig(os.path.join(PLOTS_DIR, '1_risk_distribution.png'), dpi=120)
    plt.close()
    print("  ✅ Plot 1 saved: Risk Distribution")

    # ── Plot 2: Correlation Heatmap ──
    fig, ax = plt.subplots(figsize=(7, 6))
    corr = df[FEATURE_COLS + [TARGET_COL]].corr()
    mask = np.triu(np.ones_like(corr, dtype=bool), k=1)
    sns.heatmap(corr, annot=True, fmt='.3f', cmap='RdYlGn',
                center=0, linewidths=0.8, square=True,
                annot_kws={'size': 11}, ax=ax)
    ax.set_title('Feature Correlation Heatmap', fontsize=13, fontweight='bold', pad=12)
    plt.tight_layout()
    plt.savefig(os.path.join(PLOTS_DIR, '2_correlation_heatmap.png'), dpi=120)
    plt.close()
    print("  ✅ Plot 2 saved: Correlation Heatmap")

    # ── Plot 3: NOx Trend — 1 week ──
    week = df.iloc[:168].copy()
    fig, ax = plt.subplots(figsize=(14, 4))
    ax.plot(week['Timestamp'], week['CurrentNox'],
            label='Current NOx', color='#3498db', linewidth=1.8)
    ax.plot(week['Timestamp'], week['NextHourNox'],
            label='Next Hour NOx (Actual)', color='#e74c3c',
            linewidth=1.8, linestyle='--')
    ax.axhline(y=WARNING_LIMIT,  color='#f39c12', linestyle=':', linewidth=1.5,
               label=f'Warning Limit ({WARNING_LIMIT} ppm)')
    ax.axhline(y=CRITICAL_LIMIT, color='#e74c3c', linestyle=':', linewidth=1.5,
               label=f'Critical Limit ({CRITICAL_LIMIT} ppm)')
    ax.fill_between(week['Timestamp'], 0, WARNING_LIMIT,
                    alpha=0.05, color='green', label='Safe Zone')
    ax.set_title('NOx Emission Trend — First Week of Data', fontsize=13, fontweight='bold')
    ax.set_xlabel('Date / Time')
    ax.set_ylabel('NOx Level (ppm)')
    ax.legend(loc='upper right', fontsize=9)
    ax.set_ylim(0, 165)
    plt.tight_layout()
    plt.savefig(os.path.join(PLOTS_DIR, '3_nox_trend_week.png'), dpi=120)
    plt.close()
    print("  ✅ Plot 3 saved: NOx Weekly Trend")

    # ── Plot 4: Feature vs NextHourNox ──
    fig, axes = plt.subplots(1, 4, figsize=(18, 4))
    xlabels = ['Fuel Consumption (kg/hr)', 'Production Load (%)',
               'Temperature (°C)', 'Current NOx (ppm)']
    colors  = ['#3498db', '#2ecc71', '#e67e22', '#9b59b6']
    for ax, feat, xlabel, color in zip(axes, FEATURE_COLS, xlabels, colors):
        ax.scatter(df[feat], df[TARGET_COL],
                   alpha=0.15, s=6, color=color)
        # Trend line
        z   = np.polyfit(df[feat], df[TARGET_COL], 1)
        p   = np.poly1d(z)
        xr  = np.linspace(df[feat].min(), df[feat].max(), 100)
        ax.plot(xr, p(xr), color='red', linewidth=2, label='Trend')
        corr_val = df[feat].corr(df[TARGET_COL])
        ax.set_title(f'{feat}\n(r = {corr_val:.3f})', fontsize=10, fontweight='bold')
        ax.set_xlabel(xlabel, fontsize=9)
        ax.set_ylabel('Next Hour NOx (ppm)', fontsize=9)
        ax.legend(fontsize=8)
    plt.suptitle('Feature vs Target Variable (NextHourNox)',
                 fontsize=13, fontweight='bold', y=1.02)
    plt.tight_layout()
    plt.savefig(os.path.join(PLOTS_DIR, '4_feature_scatter.png'),
                dpi=120, bbox_inches='tight')
    plt.close()
    print("  ✅ Plot 4 saved: Feature Scatter Plots")

    # ── Plot 5: Hourly average NOx pattern ──
    df['Hour'] = df['Timestamp'].dt.hour
    hourly = df.groupby('Hour')[['CurrentNox', 'NextHourNox']].mean()
    fig, ax = plt.subplots(figsize=(12, 4))
    ax.plot(hourly.index, hourly['CurrentNox'],
            marker='o', markersize=5, color='#3498db', label='Avg Current NOx')
    ax.plot(hourly.index, hourly['NextHourNox'],
            marker='s', markersize=5, color='#e74c3c',
            linestyle='--', label='Avg Next Hour NOx')
    ax.axhline(y=WARNING_LIMIT,  color='#f39c12', linestyle=':', linewidth=1.2)
    ax.axhline(y=CRITICAL_LIMIT, color='#e74c3c', linestyle=':', linewidth=1.2)
    ax.set_xticks(range(0, 24))
    ax.set_title('Average NOx by Hour of Day', fontsize=13, fontweight='bold')
    ax.set_xlabel('Hour of Day (0 = Midnight)')
    ax.set_ylabel('Average NOx (ppm)')
    ax.legend()
    plt.tight_layout()
    plt.savefig(os.path.join(PLOTS_DIR, '5_hourly_nox_pattern.png'), dpi=120)
    plt.close()
    print("  ✅ Plot 5 saved: Hourly NOx Pattern")

    print(f"\n  📁 All EDA plots saved → {PLOTS_DIR}")


# ═══════════════════════════════════════════════════════════
# SECTION 3 — PREPROCESSING
# ═══════════════════════════════════════════════════════════
def preprocess(df):
    print("\n" + "═"*55)
    print("  STEP 3 — PREPROCESSING")
    print("═"*55)

    X = df[FEATURE_COLS].values
    y = df[TARGET_COL].values

    # Time-series split — NO shuffle
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.20, shuffle=False
    )
    print(f"  ✅ Train samples : {len(X_train):,}  (80%)")
    print(f"  ✅ Test  samples : {len(X_test):,}  (20%)")
    print(f"  ✅ shuffle=False  → Time order preserved")

    # No scaling — XGBoost doesn't need it
    X_train_sc  = X_train
    X_test_sc   = X_test

    return X_train_sc, X_test_sc, y_train, y_test,


# ═══════════════════════════════════════════════════════════
# SECTION 4 — TRAINING
# ═══════════════════════════════════════════════════════════
def train(X_train, y_train):
    print("\n" + "═"*55)
    print("  STEP 4 — TRAINING XGBoost MODEL")
    print("═"*55)

    model = XGBRegressor(
        n_estimators     = 300,
        max_depth        = 6,
        learning_rate    = 0.05,
        subsample        = 0.8,
        colsample_bytree = 0.8,
        min_child_weight = 3,
        reg_alpha        = 0.1,
        reg_lambda       = 1.0,
        random_state     = 42,
        verbosity        = 0
    )

    model.fit(X_train, y_train)
    print("  ✅ Model training complete!")
    print(f"  ✅ Trees built   : {model.n_estimators}")
    print(f"  ✅ Max depth     : {model.max_depth}")
    print(f"  ✅ Learning rate : {model.learning_rate}")

    return model


# ═══════════════════════════════════════════════════════════
# SECTION 5 — EVALUATION
# ═══════════════════════════════════════════════════════════
def evaluate(model, X_test, y_test):
    print("\n" + "═"*55)
    print("  STEP 5 — MODEL EVALUATION")
    print("═"*55)

    y_pred = model.predict(X_test)

    mae  = mean_absolute_error(y_test, y_pred)
    rmse = float(np.sqrt(mean_squared_error(y_test, y_pred)))
    r2   = r2_score(y_test, y_pred)

    print(f"\n  ┌─────────────────────────────────────────┐")
    print(f"  │  MAE  (Mean Absolute Error)  : {mae:6.2f} ppm │")
    print(f"  │  RMSE (Root Mean Sq Error)   : {rmse:6.2f} ppm │")
    print(f"  │  R²   (R-Squared Score)      : {r2:8.4f}  │")
    print(f"  └─────────────────────────────────────────┘")
    print(f"\n  📌 Predictions are off by ~{mae:.1f} ppm on average")
    print(f"  📌 Model explains {r2*100:.1f}% of NOx variance")

    # Risk classification accuracy
    def classify(nox):
        if nox < SAFE_LIMIT:    return 'Safe'
        elif nox <= WARNING_LIMIT: return 'Warning'
        else:                   return 'Critical'

    actual_risk = [classify(n) for n in y_test]
    pred_risk   = [classify(n) for n in y_pred]
    risk_acc    = sum(a == p for a, p in zip(actual_risk, pred_risk)) / len(actual_risk) * 100
    print(f"  📌 Risk Label Accuracy: {risk_acc:.1f}%")

    # ── Evaluation Plots ──
    fig = plt.figure(figsize=(16, 5))
    gs  = gridspec.GridSpec(1, 3, figure=fig)

    # Plot A — Actual vs Predicted (time series)
    ax1  = fig.add_subplot(gs[0, 0:2])
    n    = 300
    ax1.plot(range(n), y_test[:n], label='Actual NOx',
             color='#3498db', linewidth=1.5, alpha=0.9)
    ax1.plot(range(n), y_pred[:n], label='Predicted NOx',
             color='#e74c3c', linewidth=1.5, linestyle='--', alpha=0.9)
    ax1.axhline(y=WARNING_LIMIT,  color='#f39c12', linestyle=':', linewidth=1.2,
                label=f'Warning ({WARNING_LIMIT}ppm)')
    ax1.axhline(y=CRITICAL_LIMIT, color='#c0392b', linestyle=':', linewidth=1.2,
                label=f'Critical ({CRITICAL_LIMIT}ppm)')
    ax1.set_title('Actual vs Predicted NOx — Test Set (first 300 samples)',
                  fontsize=11, fontweight='bold')
    ax1.set_xlabel('Test Sample Index')
    ax1.set_ylabel('NOx (ppm)')
    ax1.legend(fontsize=9)

    # Plot B — Scatter
    ax2 = fig.add_subplot(gs[0, 2])
    ax2.scatter(y_test, y_pred, alpha=0.25, s=8, color='#3498db')
    lim = [min(y_test.min(), y_pred.min()) - 5,
           max(y_test.max(), y_pred.max()) + 5]
    ax2.plot(lim, lim, 'r--', linewidth=2, label='Perfect Prediction')
    ax2.set_xlim(lim); ax2.set_ylim(lim)
    ax2.set_title(f'Actual vs Predicted\nR² = {r2:.4f}  MAE = {mae:.2f}',
                  fontsize=11, fontweight='bold')
    ax2.set_xlabel('Actual NOx (ppm)')
    ax2.set_ylabel('Predicted NOx (ppm)')
    ax2.legend(fontsize=9)

    plt.tight_layout()
    plt.savefig(os.path.join(PLOTS_DIR, '6_model_evaluation.png'), dpi=120)
    plt.close()
    print("\n  ✅ Plot 6 saved: Model Evaluation")

    # Feature importance
    fi_vals = model.feature_importances_
    fi_df   = pd.DataFrame({'Feature': FEATURE_COLS, 'Importance': fi_vals}) \
                .sort_values('Importance', ascending=True)

    fig, ax = plt.subplots(figsize=(7, 4))
    colors  = ['#3498db', '#2ecc71', '#e67e22', '#e74c3c']
    bars    = ax.barh(fi_df['Feature'], fi_df['Importance'],
                      color=colors, edgecolor='white')
    for bar, val in zip(bars, fi_df['Importance']):
        ax.text(val + 0.002, bar.get_y() + bar.get_height()/2,
                f'{val:.3f}', va='center', fontsize=10, fontweight='bold')
    ax.set_title('Feature Importance — XGBoost', fontsize=13, fontweight='bold')
    ax.set_xlabel('Importance Score')
    ax.set_xlim(0, fi_df['Importance'].max() * 1.2)
    plt.tight_layout()
    plt.savefig(os.path.join(PLOTS_DIR, '7_feature_importance.png'), dpi=120)
    plt.close()
    print("  ✅ Plot 7 saved: Feature Importance")

    print(f"\n  Feature Importance Ranking:")
    for _, row in fi_df.sort_values('Importance', ascending=False).iterrows():
        bar = '█' * int(row['Importance'] * 50)
        print(f"    {row['Feature']:20s}: {row['Importance']:.4f}  {bar}")

    return {
        'mae'          : round(float(mae), 4),
        'rmse'         : round(float(rmse), 4),
        'r2'           : round(float(r2), 4),
        'risk_accuracy': round(float(risk_acc), 2)
    }


# ═══════════════════════════════════════════════════════════
# SECTION 6 — SAVE MODEL
# ═══════════════════════════════════════════════════════════
def save_model(model, metrics):
    print("\n" + "═"*55)
    print("  STEP 6 — SAVING MODEL & SCALER")
    print("═"*55)

    joblib.dump(model,  MODEL_PATH)
    # No scaler needed
    # joblib.dump(scaler, SCALER_PATH)
    print(f"  ✅ Model  saved → {MODEL_PATH}")
    print(f"  ✅ Scaler saved → {SCALER_PATH}")

    metadata = {
        'model_name'      : 'XGBoost NOx Emission Predictor',
        'version'         : '1.0.0',
        'trained_on'      : str(pd.Timestamp.now().date()),
        'algorithm'       : 'XGBoost Regressor',
        'features'        : FEATURE_COLS,
        'target'          : TARGET_COL,
        'nox_thresholds'  : {
            'safe'    : SAFE_LIMIT,
            'warning' : WARNING_LIMIT,
            'critical': CRITICAL_LIMIT
        },
        'metrics'         : metrics,
        'hyperparameters' : {
            'n_estimators'    : 300,
            'max_depth'       : 6,
            'learning_rate'   : 0.05,
            'subsample'       : 0.8,
            'colsample_bytree': 0.8
        }
    }
    with open(METADATA_PATH, 'w') as f:
        json.dump(metadata, f, indent=4)
    print(f"  ✅ Metadata saved → {METADATA_PATH}")


# ═══════════════════════════════════════════════════════════
# SECTION 7 — QUICK PREDICTION TEST
# ═══════════════════════════════════════════════════════════
def prediction_test():
    print("\n" + "═"*55)
    print("  STEP 7 — PREDICTION TEST")
    print("═"*55)

    model_l  = joblib.load(MODEL_PATH)
    scaler_l = joblib.load(SCALER_PATH)

    def predict(fuel, load, temp, nox):
        inp    = np.array([[fuel, load, temp, nox]])
        scaled = scaler_l.transform(inp)
        pred   = float(model_l.predict(scaled)[0])
        pred   = round(pred, 2)
        if pred < SAFE_LIMIT:
            risk = 'Safe'
            msg  = f"NOx within safe limits ({pred} ppm). Continue normal operations."
        elif pred <= WARNING_LIMIT:
            risk = 'Warning'
            msg  = f"NOx approaching warning threshold ({pred} ppm). Monitor closely."
        else:
            risk = 'Critical'
            msg  = f"CRITICAL: NOx likely to breach limit ({pred} ppm). Reduce load/fuel immediately."
        return pred, risk, msg

    test_cases = [
        (310, 45, 790,  32, "Night shift — low load"),
        (450, 72, 920,  78, "Day shift  — moderate"),
        (540, 90, 1010, 118, "Peak load  — high risk"),
    ]

    for fuel, load, temp, nox, label in test_cases:
        pred, risk, msg = predict(fuel, load, temp, nox)
        icon = '🟢' if risk == 'Safe' else '🟡' if risk == 'Warning' else '🔴'
        print(f"\n  {icon} {label}")
        print(f"     Input  → Fuel:{fuel} Load:{load}% Temp:{temp}°C NOx:{nox}ppm")
        print(f"     Output → Predicted NOx: {pred} ppm  |  Risk: {risk}")
        print(f"     Alert  → {msg}")


# ═══════════════════════════════════════════════════════════
# MAIN
# ═══════════════════════════════════════════════════════════
if __name__ == '__main__':
    print("\n" + "█"*55)
    print("  AI-Based Emission Monitoring System")
    print("  ML Model Training Pipeline — Starting...")
    print("█"*55)

    df                                          = load_data()
    # run_eda(df)  # Skip EDA plots on Windows
    X_train, X_test, y_train, y_test   = preprocess(df)
    model                                       = train(X_train, y_train)
    metrics                                     = evaluate(model, X_test, y_test)
    save_model(model, metrics)
    prediction_test()

    print("\n" + "█"*55)
    print("  🎉 TRAINING PIPELINE COMPLETE!")
    print(f"  R²   : {metrics['r2']}")
    print(f"  MAE  : {metrics['mae']} ppm")
    print(f"  RMSE : {metrics['rmse']} ppm")
    print(f"  Risk Accuracy : {metrics['risk_accuracy']}%")
    print("█"*55 + "\n")
