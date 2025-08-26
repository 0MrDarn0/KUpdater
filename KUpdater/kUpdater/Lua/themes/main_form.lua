-- Hintergrund-Konfiguration
local backgroundConfig = {
    top_left = "border_top_left.png",
    top_center = "border_top_center.png",
    top_right = "border_top_right.png",
    right_center = "border_right_center.png",
    bottom_right = "border_bottom_right.png",
    bottom_center = "border_bottom_center.png",
    bottom_left = "border_bottom_left.png",
    left_center = "border_left_center.png",
    fill_color = "#101010"
}

local layoutConfig = {
    top_width_offset = 7,
    bottom_width_offset = 21,
    left_height_offset = 5,
    right_height_offset = 5,
    fill_pos_offset = 5,
    fill_width_offset = 12,
    fill_height_offset = 12
}

-- Init-Funktion
local function initWindow()
    assert(type(add_label) == "function", "add_label function is not registered")
    assert(type(get_window_size) == "function", "get_window_size function is not registered")
    assert(type(add_button) == "function", "add_button function is not registered")
    assert(type(start_game) == "function", "start_game function is not registered")
    assert(type(open_settings) == "function", "open_settings function is not registered")
    assert(type(application_exit) == "function", "application_exit function is not registered")

    local width, height = get_window_size()

    -- Titel
    add_label("kUpdater", 45, -20, "#FFA500", "Chiller", 40, "Italic")
    add_label("칼온라인", width - 115, 17, "#FFFF00", "Malgun Gothic", 11, "Bold")

    -- Close Button
    add_button("X", width - 35, 16, 18, 18, "Segoe UI", 10, "Bold", "#FFA500", "btn_exit", function() application_exit() end)

    -- Start Button
    add_button("Start", width - 150, height - 70, 97, 22, "Segoe UI", 13, "Bold", "#FFA500", "btn_default", function() start_game() end)

    -- Settings Button
    add_button("Settings", width - 255, height - 70, 97, 22, "Segoe UI", 13, "Bold", "#FFA500", "btn_default", function() open_settings() end)

end


-- Rückgabe der gesamten Fensterdefinition
return {
    background = backgroundConfig,
    layout = layoutConfig,
    init = initWindow
}