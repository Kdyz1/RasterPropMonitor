## Usage recommendations

Please, if you use this plugin to create a screen of your own, distribute a 
copy of the plugin in this fashion with your mod package:

* GameData\YourGamedataDirectory\Whatever
* GameData\JSI\RasterPropMonitor\Plugins\RasterPropMonitor.dll

and include, in whichever readme file you distribute, the link to this
GitHub repository:

https://github.com/Mihara/RasterPropMonitor/

While GPLv3 license leaves your hands free to do more or less whatever,
this way at least avoids the dll conflicts.

## Creating a display model for RasterPropMonitor

1. You need a font bitmap. The plugin treats fonts as if they were fixed
   width, so it's best to take a fixed width font to start with.
   [CBFG](http://www.codehead.co.uk/cbfg/) works, and 
   [Fixedsys Excelsior](http://www.fixedsysexcelsior.com/) is a nice font,
   but there are other programs to do the same thing and probably fonts 
   more suitable to your taste.
   
   Every letter is assumed to occupy a block of **fontLetterWidth** by
   **fontLetterHeight** pixels on the bitmap. Font texture size must be
   evenly divisible by fontLetterWidth/fontLetterHeight respectively.
   For Unity reasons, the font bitmap has to have sizes that are a power
   of 2, but it doesn't have to be square. Characters are read from the font
   left to right, top to bottom, with the bottom left character being
   character number 32 (space). Characters 128-159 are skipped due to
   peculiarities of how KSP treats strings.
   
2. You need a model for your screen. If it's to have buttons, they need to
   be named colliders and isTrigger on them enabled. The screen must be a
   named transform, arranged in such a way that the texture's 0,1
   coordinates are the top left corner of the screen. It must already have
   a texture in the layer ("\_MainTex", "\_Emissive", etc) that the plugin
   will replace. To save memory, that placeholder texture should be the
   minimum size possible, which for KSP appears to be 32x32 pixels.
   
## Configuring a monitor

Monitors are created using two modules: **RasterPropMonitor** and
**RasterPropMonitorGenerator** in a prop configuration file.
RasterPropMonitor takes care of the display, while
RasterPropMonitorGenerator feeds it with the data to display. It is perfectly
feasible to write your own plugin to completely replace
RasterPropMonitorGenerator, *(see source for helpful comments)* and do things
your way, but you probably don't want to.

### RasterPropMonitor configuration

* **screenTransform** -- the name of the screen object.
* **textureLayerID** -- Unity name of a texture ID in the object's material
  that the screen will be printed on. Defaults to "_MainTex".
* **fontTransform** -- Where to get the font bitmap. You can either place a
  texture somewhere in GameData and refer to it exactly like you would in a
  MODEL configuration node *(KSP reads in everything that looks like a texture
  and is stored outside of a PluginData directory, and assigns it an URL)*
  or put the texture on a model transform and give the name of that transform. 
* **blankingColor** -- R,G,B,A of a color that will be used to blank out the
  screen between refreshes.
* **screenWidth**/**screenHeight** -- Number of characters in a line and number
  of lines.
* **screenPixelWidth**/**screenPixelHeight** -- Width and height of the texture
  to be generated for the screen.
* **fontLetterWidth**/**fontLetterHeight** -- Width and height of a font cell
  in pixels.
* **cameraAspect** -- Aspect ratio of the camera images when this screen will
  be used to show them. *(See below for more details on cameras)*

Letters are printed on the screen in pixel-perfect mapping, so one pixel of a
font texture will always correspond to one pixel of the generated screen
texture -- as a result, you can have less characters in a line than would
fit into screenPixelWidth, but can't have more.

### RasterPropMonitorGenerator configuration

* **refreshRate** -- The screen will be redrawn no more often than once this
  number of frames.
* **refreshDataRate** -- Various computationally intensive tasks will be
  performed once this number of frames.
* **page1,page2...page8** -- Page definitions.
* **button1,button2...button8** -- Button transform names that correspond to
  pages.
* **activePage** -- Page to display on startup, 0 by default. *(Due to KSP
  limitations, currently active page cannot be made persistent without
  jumping through a lot of hoops I'm not ready for yet.)*
* **camera1,camera2...camera8** -- Names of cameras and their FOVs, if any.
  *(See the section on cameras for details)*

You need to have at least one page (page1). Clicking on button2 will cause
page2 to be rendered, etc. If there is a button option, but no corresponding
page defined, the screen will be blanked.

Pages can be defined in one of two ways -- by referencing a text file that
contains a complete screen definition, or directly in the page parameter.
You really want to use the text file, unless your line is particularly short.
Text file reference is just like a texture URL, the only difference is that
it must have a file extension.

If you wish to insert a line break in the screen definition written directly
in a prop config file, you need to replace it with "**$$$**". If you wish to
use the **{** and **}** format string characters in such a screen definition,
you need to replace **{** with **<=** and **}** with **=>**, because KSP
mangles them upon reading from prop.cfg files.

Multiple screens in the same IVA will share their computing modules, but
this also means that the lowest refreshDataRate among all those given will
be used. refreshRate remains individual per monitor.

### Screen definitions

Screen definitions are normal text files in UTF-8 encoding, lines are
separated by normal line break characters. The real power of screen
definitions comes from String.Format: various pieces of data can be inserted
anywhere into the text. For a quick reference of how String.Format works and
some examples you can see
[this handy blog post](http://blog.stevex.net/string-formatting-in-csharp/).
An example:

    Altitude is {0:##0.00} $&$ ALTITUDE

The special sequence of symbols "**$&$**" separates the text to be printed
from a space-separated list of variables to be inserted into the format
specifiers on the line. It might not be very obvious, but the first character
of the {} format specifier is the index of the variable in the list, starting
with 0.

While debugging your screen definition, it helps to know that the plugin
reloads screen definitions from disk, *(the ones stored in files, at
least)* every time it is instantiated, which is every time the vessel is
loaded -- which happens if you go back to the space center and return, or
even simply switch to an out of range vessel and back.

### Known variables

Boy, this list got long. 

I am warning you that my understanding of the mathematics involved is
practically nonexistent. If any variable isn't what you expect it should
be, please detail in what way and if possible, what should I do to fix it.

If you feel a useful variable is missing, I'm ready to add it in, provided
I don't need to write more than a page of code to acquire it.

#### Speeds

* **VERTSPEED** -- Vertical speed in m/s.
* **SURFSPEED** -- Surface speed in m/s.
* **ORBTSPEED** -- Orbital speed in m/s.
* **TRGTSPEED** -- Speed relative to target in m/s.
* **HORZVELOCITY** -- Horizontal component of surface velocity in m/s.
* **TGTRELX**, **TGTRELY**, **TGTRELZ**, -- Components of speed relative to
  target, in m/s.

#### Altitudes

* **ALTITUDE** -- Altitude above sea level in meters.
* **RADARALT** -- Altitude above the ground in meters.

#### Masses

* **MASSDRY** -- Dry mass of the ship, i.e. excluding resources.
* **MASSWET** -- Total mass of the ship.

#### Thrust and related parameters

None of these parameters know anything about vectors and orientations, mind.

* **THRUST** -- Total amount of thrust currently produced by the engines.
* **THRUSTMAX** -- Maximum amount of thrust the currently enabled engines
  can produce. 
* **TWR** -- Thrust to weight relative to the body currently being orbited
  calculated from the current throttle level.
* **TWRMAX** -- TWR you would get at full throttle.
* **ACCEL** -- Current acceleration in m/s^2
* **MAXACCEL** -- Maximum acceleration in m/s^2
* **GFORCE** -- G forces being experienced by the vessel in g.
* **THROTTLE** -- Current state of the engine throttle, a number from 0 to 1.

#### Maneuver node

* **MNODEEXISTS** -- Returns 1 if a maneuver node exists. -1 otherwise.
* **MNODETIME** -- time until/after the current maneuver node. Due to
  peculiarities of Kerbal calendar this, as well as other timespans, is
  returned as a pre-formatted string in the vein of
  `<sign><number of whole years>:<number of whole days>:<hours>:<minutes>:<seconds>.<10ths of a second>`
  MNODETIME and TIMETOPE/TARGETTIMETOPE are the only ones that come
  with a sign.
* **MNODEDV** -- Delta V remaining in the current maneuver node.

#### Orbit parameters

* **ORBITMAKESSENSE** -- Returns 1 if your vessel has an orbit to speak of, 
  -1 otherwise.
* **ORBITBODY** -- Name of the body we're orbiting.
* **PERIAPSIS** -- Periapsis of the current orbit in meters.
* **APOAPSIS** -- Periapsis of the current orbit in meters.
* **INCLINATION** -- Inclination of the current orbit in degrees.
* **ECCENTRICITY** -- Eccentricity of the current orbit.
* **ORBPERIOD** -- Period of the current orbit, a formatted timespan.
* **TIMETOAP** -- Time to apoapsis, a formatted timespan.
* **TIMETOPE** -- Time to periapsis, a formatted timespan.

#### Time

* **UT** -- Universal time.
* **MET** -- Mission Elapsed Time.

#### Names

* **NAME** -- Name of the current vessel.
* **CREW_**<*id*>**_**<**FULL**|**FIRST**|**LAST**|**PRESENT**> -- Names
  of crewmembers. IDs start with 0. I.e. for Jebediah Kerman being the
  only occupant of a capsule, CREW_0_FIRST will produce "Jebediah". An
  empty string if the seat is unoccupied. "PRESENT" qualifier is a number,
  -1 if the seat is empty and 1 if it is occupied.
* **TARGETNAME** -- Name of the target.

#### Coordinates

* **LATITUDE** -- Latitude of the vessel in degrees. Negative is south.
* **LONGITUDE** -- Longitude of the vessel in degrees. Negative is west.
* **LATITUDE_DMS**,**LONGITUDE_DMS** -- Same, but as a string converted
  to degrees, minutes and seconds.
* **LATITUDETGT**, **LONGITUDETGT**, **LATITUDETGT_DMS**, 
  **LONGITUDETGT_DMS** -- Same as above, but of a target vessel.

#### Orientation

* **HEADING**, **PITCH**, **ROLL** -- should be obvious.

#### Rendezvous and docking

* **TARGETEXISTS** -- Returns 1 if the target is a vessel, -1 if there's no
  target, and 0 if the target exists but isn't a vessel.
* **TARGETSITUATION** -- Returns the same as SITUATION but for target, if it's
  a vessel. An empty string otherwise.
* **TARGETORBITBODY** -- The name of the body your target orbits.
* **TARGETALTITUDE** -- The altitude of the target above sea level. -1 if
  there's no target.
* **TARGETDISTANCE** -- Distance to the target in meters. -1 if there's
  no target.
* **TARGETDISTANCEX**, **TARGETDISTANCEY**, **TARGETDISTANCEZ** -- Distance
  to the target separated by axis.
* **RELATIVEINCLINATION** -- Relative inclination of the target orbit. Returns
  -1 if there's no target or if the target orbits a different reference body.
* **TARGETANGLEX**, **TARGETANGLEY**, **TARGETANGLEZ** -- Angles between axes
  of the capsule and a target docking port.
* **TARGETAPOAPSIS**, **TARGETPERIAPSIS**, **TARGETINCLINATION**, 
  **TARGETECCENTRICITY**,  **TARGETORBITALVEL**,  **TARGETIMETOAP**,
  **TARGETORBPERIOD**,  **TARGETTIMETOPE**,  **TARGETTIMETOAP** -- parameters
  of the target's orbit, if one exists. Same considerations as for the
  vessel's own orbital parameters apply.

#### Resources

Notice that resource quantities are rounded down to 0.01, because otherwise
they never become properly zero, which hinders the neat formatting features.
If your resource requires a more fine grained measurement, poke me and we'll
talk about it.

* **ELECTRIC**, **ELECTRICMAX** -- Current and maximum quantity
  of ElectricCharge.
* **FUEL**, **FUELMAX** -- Same for LiquidFuel
* **OXIDIZER**, **OXIDIZERMAX** -- Same for Oxidizer
* **MONOPROP**, **MONOPROPMAX** -- Same for MonoPropellant
* **XENON**, **XENONMAX** -- Same for XenonGas

An alphabetically sorted list of all resources present in the craft is
available as well:

* **LISTR_**<*id*>**_**<**NAME**|**VAL**|**MAX**> -- where id's start with 0,
VAL is the current value and MAX is the total storable quantity, so
LISTR_0_NAME is the name of the first resource in an alphabetically
sorted list.

#### Miscellanneous

* **STAGE** -- Number of current stage.
* **SCIENCEDATA** -- Amount of science data in Mits stored in the
  entire vessel.
* **GEAR**, **BRAKES**, **SAS**, **LIGHTS**, **RCS** -- Status of the said
  systems returned as 1 if they are turned on and 0 if they are turned off.
  To format it in a smooth fashion, use a variation on {0:on;;OFF}
  format string.
* **TIMETOIMPACT** -- A very, very rough estimate of the time of contact
  with the ground. Does not currently take gravity acceleration into account.
* **SITUATION** -- Current vessel situation, i.e "flying", "orbiting", etc.
  A predefined string.

Whew, that's about all of them.

### Cameras

If a page comes with the corresponding camera option, the plugin will attempt
to find a transform by that name anywhere within the vessel, outside the IVA
-- it doesn't have to be the same part -- and place there a camera structure
that closely mimics KSP's flight camera. If the part where that transform
was found falls off, the camera should smoothly stop working. The plugin
searches it's own capsule's part first, so multiple copies of the same capsule
in the same vessel will each show their own cameras.

The picture seen from the position marked by that transform will be rendered.
Cameras will be pointing in the Z+ direction of the transform, with X+ towards
the right of the field of view. Rendering will occur at the moment when the
screen would otherwise be cleared with the blanking color. If your font has
an alpha background, it will overprint the camera image just like one would
expect.

The camera's aspect ratio is set in the RasterPropMonitor module, so you can
tune the aspect ratio to your monitor's size. 

The syntax of the camera option is `camera1 = <transform name>,<fov>`, if FOV
is not specified, a default of 60 degrees will be used.

### InternalCameraTargetHelper

A helper module included in the plugin. 

**Problem**: You wish to use an InternalCameraSwitch for docking, now that
you have that nice monitor to help you with aligning. To activate this camera,
you need to doubleclick on something. Unfortunately, doubleclick resets your
target, and you can't doubleclick again to re-target, since the camera
switched to by InternalCameraSwitch won't let you.

**Solution**: Insert `MODULE { name = InternalCameraTargetHelper }` into your
internal.cfg. Problem gone. :) It will keep track of your target and restore it
upon switching the camera in this fashion.

### JSIInternalPersistence

InternalModules can't store persistent variables normally, which, with a highly
detailed IVA like ALCOR, for which this was written, is a problem -- monitors
want to store their currently active page, switches that turn IVA lights on
and off want to store their state. This is a **PartModule** that stores this
information for them. You want to add this module to the capsule that will be
hosting the monitors and switches using JSIActionGroupSwitch. They will still
work if you don't, but currently active page numbers won't get remembered and
neither will switch states. There are no parameters to configure.

This is a highly crude solution to this problem, but it's the only one that
I was able to get to work. 

### JSIActionGroupSwitch

This module will attach an IVA prop to an action group so that clicking on
the prop will change the state of the action group as well as run an animation
on the prop. Handy for making animated switches.

A function is included to enable and disable a light on the internal model,
so you can make a light switch -- unlike action group states, the state of the
light does NOT get saved in the persistence file. Configuration options:

* **switchTransform** -- name of the transform collider on the switch that will
  trigger it. Needs to be isTrigger.
* **animationName** -- name of the animation to run. First frame of the animation
  is the 'off' position and last frame is the 'on' position.
* **reverse** -- true if the animation is to be played backwards, i.e. the "on"
  position is the first frame.
* **customSpeed** -- A speed multiplier, permitting you to play the animation
  faster or slower than it normally is. 1 is normal speed and default.
* **actionName** -- name of the action group or custom action. Valid names are:
  *gear*, *brakes*, *lights*, *rcs*, *sas*, *abort*, *stage*,
  *custom01*..*custom10*, *intlight*, *dummy*. The dummy action will produce a
  switch that animates but doesn't do anything. "intlight" action will toggle an
  internal light source.
* **internalLightName** -- name of the internal light to toggle, if needed.
  All lights sharing the same name will toggle at once.
  
### JSIPropTextureShift

This module will shift a texture on the prop it is attached to once upon startup,
and remain dormant from there on. Initially made to allow to use one model and
one texture full of button names to create lots of individual switches, it can
probably have other uses. Configuration option:

* **transformToShift** -- The name of the transform the texture on which will
  be shifted.
* **layerToShift** -- Space-separated list of the layers to operate on.
  "\_MainTex" by default.
* **x**, **y** -- offset to apply to the texture. Must be in texture coordinates,
  i.e. floats. Notice that it will be added to the offset of the texture already
  there, which is also in floats.

Once the job is done, the module selfdestructs to save memory and reduce
component count, leaving the prop with the shifted texture intact until the
next time it's instantiated.