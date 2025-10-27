local http = {}

function http.open(url)
  if not url or url == "" then
    Host.Notify("Error", "No URL provided!")
    return
  end

  local res = Website.Open(url)
  if not res.Ok then
    Host.Notify("Error", res.Error or "Failed to open URL")
  end
end

return http
