local engine = {}

function engine.start_game()
  local exe = exe_directory .. "/engine.exe"
  if not File.Exists(exe) then
    Host.Notify("Error", "engine.exe not found!")
    return
  end

  local res = Process.Start(ProcessInfo(exe, "/load /config debug"))
  if not res.Ok then
    Host.Notify("Error", res.Error or "Failed to start engine")
    return
  end

  -- only exit if start succeeded
  Application.Exit()
end

function engine.open_settings()
  local exe = exe_directory .. "/engine.exe"
  if not File.Exists(exe) then
    Host.Notify("Error", "engine.exe not found!")
    return
  end

  local res = Process.Start(ProcessInfo(exe, "/setup"))
  if not res.Ok then
    Host.Notify("Error", res.Error or "Failed to open settings")
  end
end

return engine
