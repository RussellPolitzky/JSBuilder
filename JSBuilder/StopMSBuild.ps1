# Simply stop MSBuild if its running.
Stop-Process -processname MSBuild -ErrorAction SilentlyContinue