local engine = {}

function engine.start_game()
  local exe = exe_directory .. "/engine.exe"
  if not File.Exists(exe) then
    MessageBox.Show("engine.exe not found!", "Error")
    return
  end
    Process.Start(ProcessStart(exe, "/load /config debug"))
    Application.Exit()
end

function engine.open_settings()
  local exe = exe_directory .. "/engine.exe"
  if not File.Exists(exe) then
    MessageBox.Show("engine.exe not found!", "Error")
    return
  end
  Process.Start(ProcessStart(exe, "/setup"))
end

return engine
