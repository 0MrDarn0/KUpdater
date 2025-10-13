local http = {}

function http.open(url)
  if not url or url == "" then
    MessageBox.Show("No URL provided!", "Error")
    return
  end
  Process.Start(ProcessStart(url, "", true))
end

return http
