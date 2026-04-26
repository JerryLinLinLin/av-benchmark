# Analysis outputs

- `compilation-process-comparison.md`: narrative ProcMon analysis comparing the ripgrep and Roslyn compilation traces.
- `compilation-procmon-analysis.md`: generated tables from the streamed CSV analysis, with the dynamic core process-tree view first and broad trace context later.
- `workload-profile-pipeline.md`: reproducible workload-profile report with phase detection, AV-relevance metrics, trust buckets, weighted AV-pressure scoring, reproducibility controls, and benchmark slowdown correlation.
- `procmon-summary.json`: machine-readable summary used by the markdown reports.
- `main.py`: streaming analyzer for the large ProcMon CSV files in `tmp/`; reconstructs the workload process tree from ProcMon process-create/start events.
