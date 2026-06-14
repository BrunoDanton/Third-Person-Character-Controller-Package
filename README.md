# Third-Person Character Controller Package

A modular third-person controller system for Unity.  
Includes:
- **CameraController**: orbital third-person camera with collision handling.  
- **InputManager**: centralized input mapping (Unity Input System).  
- **PlayerController**: character movement, rotation, jump, run, roll, and animations.  

---

## Installation

1. Import the scripts into your Unity project (`Scripts/Controllers`).  
2. Ensure your project uses the **Unity Input System** (Edit → Project Settings → Player → Active Input Handling → `Input System Package (New)`).  
3. Add the scripts to your player GameObject.  

---

## Setup

### Required Components
The **Player GameObject** must contain:
- `CharacterController`
- `Animator`
- `AudioSource`
- `InputManager`
- `PlayerController`

The **Camera GameObject** must contain:
- `CameraController`

---

### Input Actions
The **PlayerInput** component must reference an Input Actions asset with the following action maps:

- `Move` → `Vector2` (WASD / Left Stick)  
- `Jump` → `Button` (Space)  
- `Focus` → `Button` (Right Mouse / Gamepad Trigger)  
- `Run` → `Button` (Left Shift)  
- `Roll` → `Button` (Left Shift or separate key)  
- `Look` → `Vector2` (Mouse Delta / Right Stick)  

---

### Animator Parameters
The Animator must define these parameters:
- **Bool**: `IsGrounded`, `Move`, `Run`, `Falling`  
- **Trigger**: `Jump`, `Roll`  
- **Float**: `MovingSpeed`, `PosX`, `PosY`  

Animation events should call:
- `HandleFootstep()`
- `HandleRollSound()`
- `StopRolling()`
- `StopFootStep()`

---

### Audio
Assign audio clips in the **PlayerController** inspector:
- Footstep sound
- Roll sounds (array)
- Jump sounds (array)
- Falling loop
- Ground impact sound
- Soft landing sound

---

## Controls (Default)
- **WASD** → Move  
- **Mouse** → Look around  
- **Space** → Jump  
- **Shift (Tap)** → Roll  
- **Shift (Hold/Toggle)** → Run  
- **Right Mouse** → Focus (lock-on style rotation)  

---

## Customization
- **CameraController** → sensitivity, offset, distance, rotation limits.  
- **PlayerController** → speeds, gravity, smoothing, roll duration, animation parameters.  
- **InputManager** → easily remap to new Input Actions.  

---

## 📜 License
This package is free to use in commercial and non-commercial Unity projects.  
Attribution is appreciated but not required.  
