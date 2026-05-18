import sys
import csv
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np

UNITS = {
    "X": "kpc", "Y": "kpc", "Z": "kpc",
    "RA": "deg", "Dec": "deg",
    "Helio_Dist": "kpc", "RV": "km/s",
    "pm_ra": "mas/yr", "pm_dec": "mas/yr",
    "l": "deg", "b": "deg",
    "Release_Time": "Myr",
}


def axis_label(col):
    unit = UNITS.get(col)
    return f"{col} ({unit})" if unit else col


def main():
    csv_path, cluster_name, x_col, y_col, out_path = sys.argv[1:6]

    xs, ys, rts = [], [], []
    with open(csv_path, newline='', encoding='utf-8-sig') as f:
        for row in csv.DictReader(f):
            xs.append(float(row['x']))
            ys.append(float(row['y']))
            rts.append(float(row['release_time']))

    xs  = np.array(xs)
    ys  = np.array(ys)
    rts = np.array(rts)

    fig, ax = plt.subplots(figsize=(8, 6))
    sc = ax.scatter(xs, ys, c=rts, cmap='viridis', s=2, alpha=0.8, linewidths=0)
    cbar = fig.colorbar(sc, ax=ax)
    cbar.set_label('Release_Time (Myr)')
    ax.set_xlabel(axis_label(x_col))
    ax.set_ylabel(axis_label(y_col))
    ax.set_title(cluster_name)
    fig.tight_layout()
    fig.savefig(out_path, dpi=150)
    print(f"Saved: {out_path}")


if __name__ == '__main__':
    main()
