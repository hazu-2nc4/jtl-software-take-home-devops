# JtlDemo dependency-analysis cache

This directory is a source snapshot, not a build input. It lets later migration
work begin from the endpoint and Windows-dependency inventory without repeating
the initial traversal.

| File | Use |
| --- | --- |
| `endpoint-dependency-graph.json` | Authoritative, machine-readable endpoint, project, and OS-dependency graph. |
| `endpoint-dependency-graph.dot` | Renderable Graphviz view of endpoint paths and Windows boundaries. |
| `windows-migration-audit.md` | Human-readable findings and blast-radius assessment. |

## Validity

Generated from the 14 handwritten `*.cs` and `*.csproj` files plus
`JtlDemo.sln` under `senior-devops-engineer/app` on 2026-09-06. Generated
`bin/` and `obj/` output was excluded from source traversal; restored package
metadata was inspected separately with `dotnet list package --include-transitive`.
The JSON `source_files` inventory contains SHA-256 values. Treat this snapshot
as stale and regenerate the analysis if any listed file or the solution hash
changes. The graph stops at the public .NET API calls named in the source; it
does not claim to decompile framework/native implementations.
