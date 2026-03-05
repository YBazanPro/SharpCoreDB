# SharpCoreDB Benchmark Suite

Benchmark harness for comparing SharpCoreDB against BLite and Zvec.

## Quick Start

```bash
cd tests/benchmarks

# Run all benchmarks
dotnet run --project SharpCoreDB.Benchmarks/SharpCoreDB.Benchmarks.csproj

# Results will be in: results/YYYY-MM-DD-HHMMSS/
```

## Structure

```
tests/benchmarks/
├── BenchmarkConfig.json                # Scenario configuration
├── README.md                           # This file
├── SharpCoreDB.Benchmarks/
│   ├── SharpCoreDB.Benchmarks.csproj
│   ├── Program.cs                      # Entry point
│   ├── BLite/
│   │   ├── BliteCrudBenchmark.cs      # B1-B4 scenarios (Week 4+)
│   │   └── BliteMixedWorkloadBenchmark.cs
│   └── Zvec/
│       ├── ZvecIndexBuildBenchmark.cs  # Z1-Z5 scenarios (Week 4+)
│       └── ZvecQueryBenchmark.cs
├── harness/
│   ├── dataset-generator.ps1           # Generate test data
│   ├── hardware-profile.ps1            # Capture hardware info
│   └── report-generator.ps1            # Generate reports
├── results/
│   └── [YYYY-MM-DD-HHMMSS]/
│       ├── raw-data.json               # All measurements
│       ├── environment.json            # Hardware/runtime snapshot
│       ├── report.md                   # Summary (generated)
│       └── raw-csv/                    # Per-scenario CSV exports
└── docs/
    ├── METHODOLOGY.md                  # See docs/benchmarks/BENCHMARK_METHOD.md
    ├── REPRODUCING.md                  # Step-by-step rerun guide
    └── INTERPRETING_RESULTS.md         # FAQ
```

## Week 3 Status

✅ **Scaffolded:**
- Benchmark harness project
- Configuration framework
- Result serialization
- Runner entry point

⏳ **TODO (Weeks 4-5):**
- BLite scenario implementations (B1-B4)
- Zvec scenario implementations (Z1-Z5)
- Report v1 generation
- CI integration

## Configuration

Edit `BenchmarkConfig.json` to enable/disable scenarios and adjust parameters.

## Results

Raw results are saved to `results/[run-date]/raw-data.json` and `results/[run-date]/raw-csv/`.

See `docs/benchmarks/` for methodology and interpretation guides.
