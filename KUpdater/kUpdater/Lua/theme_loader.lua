local current_theme = {}

function load_theme(name)
    assert(type(name) == "string", "Theme name must be a string")

    local path = THEME_DIR .. "/" .. name .. ".lua"
    local ok, result = pcall(dofile, path)

    if ok and type(result) == "table" then
        current_theme = result
    else
        print("Failed to load theme:", result)
        current_theme = {}
    end
end

function get_theme()
    return current_theme
end
