# Avatar-ConversationAR

# Avatar-ConversationAR

## 🧩 System Requirements

### Development Environment

- **Unity Version:** Unity 6.0 (6000.0.49f1) or newer

### Target Devices

- **iOS:** iPhone/iPad with iOS 13.0+ (ARKit compatible)
- **Android:** Android 7.0+ with ARCore support
- **Processor:** ARM64 architecture recommended

---

## ⚙️ Pre-Build Setup

### 1. Unity Configuration

**Build Settings:**

- Platform: iOS or Android
- Architecture: ARM64
- Scripting Backend: IL2CPP
- API Compatibility Level: .NET Standard 2.1

---

### 2. Required API Keys

#### Gemini AI API Key (for avatar conversations)

1. Visit [Google AI Studio](https://makersuite.google.com/)
2. Generate API key
3. In Unity, go to `AvatarConversationManager` and assign it to the `geminiApiKey` field

#### Ready Player Me (Optional)

- No API key required for basic avatar setup

---

### 3. Package Dependencies

Install via **Window → Package Manager**:

- AR Foundation (6.1.0+)
- XR Plugin Management (4.5.1+)
- ARCore XR Plugin (for Android)
- ARKit XR Plugin (for iOS)

---

## 🚀 Build Process

### iOS Build

1. **Unity Settings**

   - Switch platform to iOS
   - Player Settings → iOS:
     - Bundle Identifier: `com.yourname.arbasketball`
     - Minimum iOS Version: 13.0
     - Architecture: ARM64
     - Camera Usage Description: _"Required for AR features"_
     - Microphone Usage Description: _"Required for voice commands"_

2. **Build Steps**

   - Click **Build** and choose output folder
   - Open the generated Xcode project
   - Connect your iOS device
   - Build and run from Xcode

3. **Xcode Configuration**
   - Select your Development Team
   - Enable _“Automatically manage signing”_
   - Ensure device is registered for development

---

### Android Build

1. **Unity Settings**

   - Switch platform to Android
   - Player Settings → Android:
     - Package Name: `com.yourname.arbasketball`
     - Minimum API Level: 24 (Android 7.0)
     - Target API Level: 33
     - Scripting Backend: IL2CPP
     - Architecture: ARM64

2. **Build Steps**
   - Click **Build** or **Build and Run**
   - Install the APK on an ARCore-supported Android device

---

## 📲 Running the App

### First Launch

- Grant permissions for:
  - **Camera access** (required)
  - **Microphone access** (for voice features)
- Ensure:
  - Good lighting
  - Camera is pointed at flat surfaces (e.g., table or floor)
  - 2–3 seconds allowed for plane detection

---

## 🎮 App Usage

### 1. Basketball Scene

- Launch app → Select **Play Basketball**
- Point at a flat surface → Wait for plane detection
- Tap to place basketball hoop
- Use UI buttons:
  - **Animation Toggle:** Start/stop automatic ball shooting
  - **Physics Toggle:** Switch between realistic and arcade-style physics
  - **Manual Shoot:** Shoot manually

---

### 2. Play with Avatar Scene

- Launch app → Select **Play with Avatar**
- Point at a flat surface → Wait for detection
- Tap to place animated avatar with basketball hoop
- Use UI buttons:
  - **Animation Toggle:** Avatar shoots ball automatically
  - **Physics Toggle:** Switch between realistic and arcade-style physics
  - **Manual Shoot:** Manual shooting by avatar

---

### 3. Avatar Conversation Scene

- Launch app → Select **Talk with Avatar**
- Point at a flat surface → Tap to place avatars
- Press **Record**:
  - Male avatar starts listening
- Speak your question → Release to stop recording
- Wait for response → Female avatar speaks the reply

---

## 🧪 Troubleshooting

### App Crashes on Launch

- Confirm ARCore/ARKit support
- Ensure device meets minimum OS/version requirements

### Plane Detection Not Working

- Improve lighting
- Avoid plain white or reflective surfaces
- Move device slowly to help scan the environment

### Voice Features Not Working

- Check microphone permission
- Verify Gemini API key setup
- Confirm active internet connection

### Animations Not Playing

- Ensure animation clips are set to **Legacy**
- Confirm correct Animation components are assigned
- Check the Unity console for errors

---

## 🎮 Usage Guide

### 🧭 Main Menu Navigation

- **Simple Basketball**: Basic shooting (no avatar)
- **Avatar Basketball**: Avatar-controlled basketball
- **Avatar Conversation**: AI voice chat with avatars
- **Settings / Exit**: App settings or exit

---

### 🏀 Simple Basketball

1. Select **"Simple Basketball"**
2. Move device to detect a flat surface
3. Tap to place basketball hoop
4. Use UI:
   - **Animation Toggle** – Auto shooting
   - **Physics Toggle** – Realistic vs arcade mode
   - **Manual Shoot** – Single ball shot

---

### 🧍 Avatar Basketball

1. Select **"Avatar Basketball"**
2. Tap to place setup on detected surface
3. Avatar performs:
   - Walk → Pickup → Aim → Throw → Celebrate
4. UI Controls:
   - **Animation Toggle** – Start/stop avatar sequence
   - **Physics Toggle** – Realistic or arcade physics
   - **Manual Shoot** – Trigger avatar play

---

### 🗣️ Avatar Conversation

1. Select **"Avatar Conversation"**
2. Tap to place male & female avatars
3. Press **Record** – Male avatar listens
4. Speak → Release to submit
5. Female avatar replies with animated speech

---

## ⚙️ Configuration Options

### Basketball Settings (`SimpleARBasketball.cs`)

```csharp
public float shootForce = 12f;
public float upwardForce = 8f;
public float shootInterval = 4f;

🔧 Conversation Settings (AvatarConversationManager.cs)

public string geminiApiKey;
public bool enableVoiceResponse;
public string talkingBoolParameter;


Development Notes
Dependencies
AR Foundation – Core AR logic

Whisper Unity – Voice input

DOTween – UI animations

Gemini API – Text-to-text AI

SimpleWebTTS – Text-to-speech audio

Key Components
ARRaycastManager – Tap-to-place logic

ARPlaneManager – Plane detection

AROcclusionManager – Depth-based rendering

Animation – Legacy animation handling

UI System – World-space canvas controls

Performance Considerations
All animations use Legacy format

Optimized physics with toggled gravity

DOTween for lightweight UI transitions

Streaming audio for real-time feedback
```
