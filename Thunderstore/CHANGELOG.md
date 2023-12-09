## 1.1.2

### Fixes

- Fixed support for BCP 3.0.0+<br/>
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