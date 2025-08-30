-- Hintergrund-Konfiguration
local background_config = {
    top_left = "main_tl_default.png",
    top_center = "main_tc_default.png",
    top_right = "main_tr_default.png",
    right_center = "main_rc_default.png",
    bottom_right = "main_br_default.png",
    bottom_center = "main_bc_default.png",
    bottom_left = "main_bl_default.png",
    left_center = "main_lc_default.png",
    fill_color = "#101010"
}

local layout_config = {
    top_width_offset = 7,
    bottom_width_offset = 21,
    left_height_offset = 5,
    right_height_offset = 5,
    fill_pos_offset = 5,
    fill_width_offset = 12,
    fill_height_offset = 10
}

local function get_color(name)
    local color_table = {
        black       = "#000000",
        white       = "#ffffff",
        red         = "#ff0000",
        lime        = "#00ff00",
        blue        = "#0000ff",
        yellow      = "#ffff00",
        cyan        = "#00ffff",
        magenta     = "#ff00ff",
        silver      = "#c0c0c0",
        gray        = "#808080",
        maroon      = "#800000",
        olive       = "#808000",
        green       = "#008000",
        purple      = "#800080",
        teal        = "#008080",
        navy        = "#000080",
        orange      = "#ffa500",
        gold        = "#ffd700",
        pink        = "#ffc0cb",
        brown       = "#a52a2a",
        chocolate   = "#d2691e",
        coral       = "#ff7f50",
        crimson     = "#dc143c",
        indigo      = "#4b0082",
        ivory       = "#fffff0",
        khaki       = "#f0e68c",
        lavender    = "#e6e6fa",
        orchid      = "#da70d6",
        plum        = "#dda0dd",
        salmon      = "#fa8072",
        tomato      = "#ff6347",
        turquoise   = "#40e0d0",
        violet      = "#ee82ee"
    }
    return color_table[string.lower(name)] or "#FFFFFF" -- fallback: white
end

function reload_theme()
   assert(type(reinit_theme) == "function", "reinit_theme function is not registered")
   reinit_theme()
end

-- Init-Funktion
local function init_window()
    assert(type(add_label) == "function", "add_label function is not registered")
    assert(type(get_window_size) == "function", "get_window_size function is not registered")
    assert(type(add_button) == "function", "add_button function is not registered")
    assert(type(start_game) == "function", "start_game function is not registered")
    assert(type(open_settings) == "function", "open_settings function is not registered")
    assert(type(application_exit) == "function", "application_exit function is not registered")
    assert(type(open_website) == "function", "open_website function is not registered")

    local width, height = get_window_size()

    local titel_color = get_color("orange")
    local titel_font = "Chiller"
    local titel_font_size = 40
    local titel_font_style = "Italic"

    local subtitle_color = get_color("gold")
    local subtitle_font = "Malgun Gothic"
    local subtitle_font_size = 13
    local subtitle_font_style = "Bold"

    local btn_color = get_color("orange")
    local btn_font = "Segoe UI"
    local btn_font_size = 11
    local btn_font_style = "Regular"

    -- Title
    add_label("lb_title", "kUpdater", 45, -10, titel_color, titel_font, titel_font_size, titel_font_style)
    add_label("lb_subtitle", "칼온라인", width - 115, 12, subtitle_color, subtitle_font, subtitle_font_size, subtitle_font_style)

    add_button("btn_close", "X", width - 35, 16, 18, 18, btn_font, 10, btn_font_style, btn_color, "btn_exit", function() application_exit() end)
    add_button("btn_start", "Start", width - 150, height - 70, 97, 22, btn_font, btn_font_size, btn_font_style, btn_color, "btn_default", function() start_game() end)
    add_button("btn_settings", "Settings", width - 255, height - 70, 97, 22, btn_font, btn_font_size, btn_font_style, btn_color, "btn_default", function() open_settings() end)
    add_button("btn_website", "Website", width - 360, height - 70, 97, 22, btn_font, btn_font_size, btn_font_style, btn_color, "btn_default", function() open_website("https://google.com") end)

end


-- Rückgabe der gesamten Fensterdefinition
return {
    background = background_config,
    layout = layout_config,
    init = init_window
}