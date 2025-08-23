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
    button = {
        normal = "#B22222",
        hover = "#DC143C",
        pressed = "#8B0000",
        font_color = "#FFFFFF",
        font = {
            name = "Segoe UI",
            size = 14,
            style = "Regular"
        }
    },
    init = function()
        add_text("칼온라인", FrameWidth - 115, 17, "#FFFF00", "Chiller", 13, "Bold")
    end
}
