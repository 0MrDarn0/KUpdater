local util = {}

function util.safe_get(t, key, default)
  if t == nil then return default end
  local ok, v = pcall(function() return t[key] end)
  if not ok then return default end
  if v == nil then return default end
  return v
end

return util






