## Easy Snap Version 1.0.0
############################

# How It Works

- All objects will snap to the grid automatically while the Easy Snap window is open

- Snapping can be toggled on/off using the grid icon

- A coloured grid is drawn whenever the editor camera is in one of the isometric views (top/right/front)

- The grid visualization can be toggled using the eye icon

- Grid snapping can be temporarily disabled by holding down the "Snap Override" key (default: Spacebar)

- The grid can be sized on all three axes independently in the Easy Snap window

- The units can be changed quickly under the grid size fields in the Easy Snap window. Custom units can
be set up in the settings

- Objects that become misaligned with the grid can be re-aligned by pressing the "Align" button or by
using the keyboard shortcut (default: Enter/Return)

- Custom units can be defined relative to other units (e.g. 1 centimeter = 0.01 unity units, 1 meter = 100 centimeters)
Easy Snap handles all of the conversions automatically. The only restriction is that you can't have cyclic references -
setting 1m = 100cm and 1cm = 0.01m will not work, the plugin won't give you the option to do this


# Settings

Enable Major Grid 	- Toggle visualization of major grid lines in isometric views. Major grid lines are purely visual
Major Grid Size		- The size (in minor grid cells) that the major grid should be
Draw Axes			- Toggle the visualization of the three axes at the origin in isometric views
Button Size			- Changes the size of the buttons displayed in the Easy Snap toolbar


Feel free to forward any questions to support@aegongames.com