import pandas as pd
import numpy as np
from datetime import datetime, timedelta

np.random.seed(42)
TOTAL_HOURS = 8760
START_DATE  = datetime(2024, 1, 1, 0, 0, 0)

timestamps = [START_DATE + timedelta(hours=i) for i in range(TOTAL_HOURS)]
hours  = [t.hour  for t in timestamps]
months = [t.month for t in timestamps]

def get_production_load(hour, month):
    seasonal = 4.0 if month in [4,5,6,7,8] else 0.0
    if   0  <= hour <= 5:  base = np.random.uniform(40, 58)
    elif 6  <= hour <= 8:  base = np.random.uniform(58, 72)
    elif 9  <= hour <= 17: base = np.random.uniform(72, 100)
    elif 18 <= hour <= 21: base = np.random.uniform(60, 78)
    else:                  base = np.random.uniform(48, 62)
    return min(100.0, round(base + seasonal + np.random.normal(0, 2), 2))

production_loads = [get_production_load(h, m) for h, m in zip(hours, months)]

def get_fuel_consumption(load):
    base  = 200 + (load / 100) * 400
    noise = np.random.normal(0, 14)
    return round(max(200, min(600, base + noise)), 2)

fuel_consumptions = [get_fuel_consumption(l) for l in production_loads]

def get_temperature(load):
    base  = 600 + (load / 100) * 500
    noise = np.random.normal(0, 22)
    return round(max(600, min(1100, base + noise)), 2)

temperatures = [get_temperature(l) for l in production_loads]

def get_current_nox(fuel, load, temp):
    # Direct formula — realistic range 20-150 ppm
    base  = (fuel * 0.12) + (load * 0.85) + (temp * 0.06) - 80
    noise = np.random.normal(0, 8)
    return round(max(20, min(150, base + noise)), 2)

current_nox_values = [
    get_current_nox(f, l, t)
    for f, l, t in zip(fuel_consumptions, production_loads, temperatures)
]

def get_next_hour_nox(current_nox, fuel, load, temp):
    """
    NextHourNox seedha CurrentNox ke upar based hai.
    High current NOx = High predicted NOx — guaranteed.
    """
    # Base = weighted average of current nox and operating conditions
    operating_nox = (fuel * 0.12) + (load * 0.85) + (temp * 0.06) - 80
    base  = (current_nox * 0.75) + (operating_nox * 0.25)
    noise = np.random.normal(0, 6)
    spike = np.random.choice([0, np.random.uniform(10, 25)], p=[0.95, 0.05])
    result = base + noise + spike
    return round(max(20, min(160, result)), 2)

next_hour_nox_values = [
    get_next_hour_nox(n, f, l, t)
    for n, f, l, t in zip(current_nox_values, fuel_consumptions,
                           production_loads, temperatures)
]

def get_risk_label(nox):
    if nox < 80:     return 'Safe'
    elif nox <= 100: return 'Warning'
    else:            return 'Critical'

risk_labels = [get_risk_label(n) for n in next_hour_nox_values]

df = pd.DataFrame({
    'Timestamp'       : timestamps,
    'FuelConsumption' : fuel_consumptions,
    'ProductionLoad'  : production_loads,
    'Temperature'     : temperatures,
    'CurrentNox'      : current_nox_values,
    'NextHourNox'     : next_hour_nox_values,
    'RiskLabel'       : risk_labels
})

df.to_csv('synthetic_plant_data.csv', index=False)

print(f"✅ Dataset: {len(df)} rows")
print(f"\nValue Ranges:")
for c in ['FuelConsumption','ProductionLoad','Temperature','CurrentNox','NextHourNox']:
    print(f"  {c:20s}: {df[c].min():.1f} – {df[c].max():.1f}  (mean: {df[c].mean():.1f})")
print(f"\nRisk %:")
print(df['RiskLabel'].value_counts(normalize=True).mul(100).round(1))