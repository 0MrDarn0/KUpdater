local current_theme = {}

function load_theme(name)
    local path = THEME_DIR .. "/" .. name .. ".lua"
    current_theme = dofile(path)
end

function get_theme()
    return current_theme
end
