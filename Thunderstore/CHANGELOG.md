## 2.1.1

### Fixes

- Fixed BCP 3.4.0 compatibility<br/>
  **Emergency fix.** Seems like something went wrong in BCP while bumping
  the version number, which this mod checks for to ensure compatibility.
  This check has been temporarily removed until it's fixed in BCP.

## 2.1.0

### Features

- Added host-only `forceevent` command<br/>
  Allows the host in your game to force an event to happen next round.

### Fixes

- Fixed BCP 3.4.0 compatibility<br/>
  Event weights were added in BCP 3.4.0, which broke this mod.<br/>
  Currently, this mod will not use the event weights because I'm working together
  with Nips to get an official event API in BCP. That should also fix any future incompatibility issues.

## 2.0.1

### Fixes

- Properly sync turret damage to clients<br/>
  Prevents clients from getting the wrong amount of turret damage.

## 2.0.0

Sorry that it took me so long to add a new event, because somehow I thought the hardest event to make would be the
easiest. Oh, how wrong I was...

Anyhow, besides the new event, events are now also networked (which is the major update),
allowing for more interesting events in the future!
Next up will be the `It's just a burning memory` event.

### Features

- Added `Since when can they move?!` event<br/>
  *Did someone give them wheels?*<br/>
  Featuring custom AI and a whole lot of chaos!<br/>
  **If you find this event too easy or too hard, please let me know! I'm still tweaking it.**

## 1.1.3

### Fixes

- Fixed BCP 3.3.0 compatibility<br/>
  Some changes were made in BCP 3.3.0 that broke this mod.

## 1.1.2

### Fixes

- Fixed support for BCP 3.0.0+ (thank you Tahdikas for helping!)<br/>
  Some leftover code from pre-1.1.0 broke when certain events were disabled in BCP.

## 1.1.1

### Fixes

- Initialize BCManager at Start<br/>
  Should hopefully fix a `NullReferenceException` when loading the world in specific circumstances.

## 1.1.0

### Features

- Added event configuration<br/>
  You can now configure which event is enabled or not.

### Fixes

- Fixed BCP 3.0.0+ compatibility<br/>
  Now handles the event configuration of BCP 3.0.0+ correctly.

## 1.0.0

### Features

- Added `Bling bling` event
- Added `Got any stock?` event