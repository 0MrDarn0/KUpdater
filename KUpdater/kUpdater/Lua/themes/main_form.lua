-- Hintergrund-Konfiguration
local background_config = {
  top_left      = "main_tl_default.png",
  top_center    = "main_tc_default.png",
  top_right     = "main_tr_default.png",
  right_center  = "main_rc_default.png",
  bottom_right  = "main_br_default.png",
  bottom_center = "main_bc_default.png",
  bottom_left   = "main_bl_default.png",
  left_center   = "main_lc_default.png",
  fill_color    = "#101010"
}

local layout_config = {
  top_width_offset    = 7,
  bottom_width_offset = 15,
  left_height_offset  = 5,
  right_height_offset = 5,
  fill_pos_offset     = 5,
  fill_width_offset   = 12,
  fill_height_offset  = 10
}


local function bounds(x, y, w, h)
  return function()
    local width, height = get_window_size()
    local rx = (x < 0) and (width + x) or x
    local ry = (y < 0) and (height + y) or y
    return { x = rx, y = ry, width = w, height = h }
  end
end

-- Generische Anchor-Funktion
-- mode: "top_left", "top_right", "bottom_left", "bottom_right"
local function anchor(x, y, w, h, mode)
  mode = mode or "top_left"

  return function()
    local winW, winH = get_window_size()

    local rx, ry, rw, rh

    -- Breite
    if w < 0 then
      rw = winW + w - ((x < 0) and (winW + x) or x)
    else
      rw = w
    end

    -- Höhe
    if h < 0 then
      rh = winH + h - ((y < 0) and (winH + y) or y)
    else
      rh = h
    end

    if mode == "top_left" then
      rx = (x < 0) and (winW + x) or x
      ry = (y < 0) and (winH + y) or y

    elseif mode == "top_right" then
      rx = winW - ((x < 0) and -x or x) - rw
      ry = (y < 0) and (winH + y) or y

    elseif mode == "bottom_left" then
      rx = (x < 0) and (winW + x) or x
      ry = winH - ((y < 0) and -y or y) - rh

    elseif mode == "bottom_right" then
      rx = winW - ((x < 0) and -x or x) - rw
      ry = winH - ((y < 0) and -y or y) - rh
    end

    return { x = rx, y = ry, width = rw, height = rh }
  end
end

local engine = require("actions.engine")
local http = require("actions.http")

-- Rückgabe der gesamten Fensterdefinition
return {
  background = background_config,
  layout     = layout_config,

  init = function()
  
    -- Title
    local titleLabel = UILabel("lb_title",
        bounds(35, 0, 200, 40),
        T("app.title"),
        Font("Chiller", 40, "Italic"),
        Color.Orange)
    uiElement.Add(titleLabel)

    local subtitleLabel = UILabel("lb_subtitle",
        bounds(-115, 12, 200, 27),
        T("app.subtitle"), 
        Font("Malgun Gothic", 13, "Bold"),
        Color.Gold)
    uiElement.Add(subtitleLabel)

    -- Buttons
    local btnClose = UIButton("btn_close",
      bounds(-35, 16, 18, 18),
      T("button.exit"),
      Font("Segoe UI", 10, "Regular"),
      Color.Orange,
      "btn_exit",
      function() application_exit() end)
    uiElement.Add(btnClose)

    local btnStart = UIButton("btn_start",
      bounds(-150, -70, 97, 22),
      T("button.start"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "btn_default",
      function() engine.start_game() end)
    uiElement.Add(btnStart)

    local btnSettings = UIButton("btn_settings",
      bounds(-255, -70, 97, 22),
      T("button.settings"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "btn_default",
      function() engine.open_settings() end)
    uiElement.Add(btnSettings)

    local btnWebsite = UIButton("btn_website",
      bounds(-360, -70, 97, 22),
      T("button.website"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "btn_default",
      function() http.open("https://google.com") end)
    uiElement.Add(btnWebsite)


-- Progressbar (27px vom linken Rand, 30px vom unteren Rand,
-- rechts -27px Abstand, Höhe 5px)
    local progressBar = UIProgressBar("pb_update_progress",
      anchor(27, 30, -27, 5, "bottom_left"))
      uiElement.Add(progressBar)


    local changelogBox = UITextBox("tb_changelog", 
        anchor(36, 55, -400, 200, "bottom_left"),
        "Changelog ...",
        Font("Segoe UI", 10, "Regular"),
        Color.White, MakeColor.FromHex("#101010"),
        true, true, MakeColor.FromHex("#7C6E4B"))

        -- Rahmen + Glow konfigurieren
        changelogBox.BorderColor = MakeColor.FromHex("#7C6E4B")
        changelogBox.BorderThickness = 3
        changelogBox.GlowEnabled = true
        changelogBox.GlowColor = Color.White
        changelogBox.GlowRadius = 6

    uiElement.Add(changelogBox)


    -- Status-Label (27px vom linken Rand, 50px vom unteren Rand)
    local statusLabel = UILabel("lb_update_status",
      anchor(27, 20, 200, 20, "bottom_left"),
      T("status.waiting"),
      Font("Segoe UI", 8, "Italic"),
      Color.White)
      uiElement.Add(statusLabel)

  end,


  on_update_status = function(message)
    update_status(message)
  end,


  on_update_progress = function(value)
    update_download_progress(value)
  end

}
