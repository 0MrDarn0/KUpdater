return {
    window_title = "kUpdater",
    font_color = "#FFA500",
    title_position = { x = 40, y = -10 },
    title_font = {
        name = "Chiller",
        size = 40,
        style = "Italic"
    },
    background = {
        top_left = "border_top_left.png",
        top_center = "border_top_center.png",
        top_right = "border_top_right.png",
        right_center = "border_right_center.png",
        bottom_right = "border_bottom_right.png",
        bottom_center = "border_bottom_center.png",
        bottom_left = "border_bottom_left.png",
        left_center = "border_left_center.png",
        fill_color = "#101010"
    },
    init = function()
        -- Make sure the required functions are available
        assert(type(add_label) == "function", "add_label function is not registered")
        assert(type(get_window_size) == "function", "get_window_size function is not registered")
        
          -- Get the current window size
        local width, height = get_window_size()

          -- Add a custom label to the window
        add_label("칼온라인", width - 115, 17, "#FFFF00", "Chiller", 13, "Bold")

        add_button(
            "Exit",          -- Text
            765, 16,         -- X, Y
            18, 18,          -- Breite, Höhe
            "Segoe UI", 10,  -- Schriftart, Größe
            "Bold",          -- Schriftstil
            "#FF0000",       -- Farbe
            "btn_exit",      -- ID
            function()       -- Klick-Callback
                print("Exit clicked")
            end
        )


    end
}
