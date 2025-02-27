QuestVisionKit is a collection of template and reference projects demonstrating how to use Meta Quest’s new Passthrough Camera API for advanced AR/VR vision, tracking, and shader effects.

Overview
========

The repository includes the following sample projects:

1. 🟢 Color Picker
   --------------------------------
   - **Purpose:** Convert a 3D point in space to its corresponding 2D image pixel.
   - **Description:** This sample shows the mapping between 3D space and 2D image coordinates using the Passthrough Camera API. We use MRUK's EnvironmentRaycastManager to determine a 3D point in our environment and map it to the location on our WebcamTexture. We then extract the pixel on that point, to determine the color of a real world object.
| Color Picker (Environment Mode)            | Color Picker (Manual Mode)                 |
|--------------------------------------------|--------------------------------------------|
| ![CPE](Media/ColorPicker_Environment.gif)  | ![CPM](Media/ColorPicker_Manual.gif)       |


2. 🟡 Object Detection with Unity Sentis - Human (or animal) intrusion detection for safety
   --------------------------------
   - **Purpose:** Use the Unity Sentis framework to run different CV model to detect and track objects.
   - **Description:** Learn how to convert detected image coordinates (e.g. bounding boxes) back into 3D points for dynamic interaction within your scenes. Demonstrate a practical example of image tracking applied to safety-critical scenarios. This sample shows real-time detection and tracking of persons or animals, which can be used to trigger safety alerts or automated responses.

3. 🟡 QR Code Tracking with ZXing
   --------------------------------
   - **Purpose:** Detect and track QR codes in real time.
   - **Description:** Similarly to the object detection sample, get QR code coordinated and projects them into 3D space. Detect QR codes and call their URLs.

4. 🔴 Frosted Glass Shader
   --------------------------------
   - **Purpose:** Apply a custom frosted glass shader effect to virtual surfaces.
   - **Description:** A shader which takes our camera feed as input to blur the content behind it. I know you want the Apple Vision Pro forsted glass 👀

5. 🔴 OpenAI vision model & voice commands
   --------------------------------
   - **Purpose:** Ask OpenAI's vision model (or any other multi-modal LLM) for context of your current scene.
   - **Description:** In this sample we implement a simple connection to OpenAI's vision model. Additionally it is connected to Meta's Voice SDK for easy voice commands. The goal is to send an image to an LLM, using a simple wake workd and voice command, to quickly and seamleslly get an answer.
  
Status Legend
=============================
- 🟢 **Green:** Fully Implemented – This sample is complete and works as expected.
- 🟡 **Yellow:** Known Issues or Limitations – The sample is not fully implemented yet or has some known issues.
- 🔴 **Red:** Work in Progress (WIP) – The sample is currently under active development.

Getting Started
===============

Prerequisites:
--------------
- **Meta Quest Device:** Ensure your device is updated to firmware v74 or later.
- **Unity:** Recommended version 6.
- **Passthrough Camera API:** Follow Meta Quest’s documentation to enable and configure the API. The API is part of the Meta XR Core SDK v74 or later.
- **Sentis Framework:** For image tracking samples, refer to the Sentis documentation for installation and setup.
- **Camera Passthrough API does not work in the Editor or XR Simulator.**

Installation:
-------------
1. **Clone the Repository:**
   Run the following command in your terminal:
git clone https://github.com/yourusername/QuestVisionKit.git

2. **Open the Project in Unity:**
Launch Unity and open the cloned project folder.

3. **Configure Dependencies:**
Follow the instructions in the section below to run one of the samples.

Running the Samples
===================

- **Color Picker**
  - Open the `ColorPicker` scene and run the application.
  - Build the scene and run the APK on your headset.
  - Use your controller, aim the ray onto a surface in your real space and press the trigger to observe the corresponding pixel from the passthrough camera feed.

- **Object Detection with Unity Sentis**
  - Open the `ObjectDetection` scene.
  - You will need Unity Sentis for this project to run (com.unity.sentis).
  - Select the labels you would like to track. No label means all objects will be tracked.
  - Build the scene and run the APK on your headset. Look around your room and see how tracked objects receive a bounding box in accurate 3D space.

- **QR Code Tracking**
  - Open the `QRCodeTracking` scene to test real-time QR code detection and tracking.
  - You will need to install [NuGet for Unity]()
  - After install you will have a new Menu `NuGet`. Click on it and then on `Manage NuGet Packages`. Search for the [ZXing.Net package](https://github.com/micjahn/ZXing.Net/) from Michael Jahn and install it.
  - Build the scene and run the APK on your headset. Look at a QR code to see the marker in 3D space and URL of the QR code.

- **Frosted Glass Shader**
  - Open the `FrostedGlass` scene.
  - Build the scene and run the APK on your headset.
  - Look at the panel from different angles and observe how objects behind it are blurred.

- **OpenAI vision model & voice commands**
   - Open the `OpenAIVision` scene.
   - Build the scene and run the APK on your headset.

License
=======

This project is licensed under the MIT License. See the LICENSE file for details. Feel free to use the samples for your own projects, though I would appreciate if you would leave some credits in your work.

Contact
=======

For questions, suggestions, or feedback, please open an issue in the repository or contact me on [X](https://x.com/xrdevrob), [LinkedIn](https://www.linkedin.com/in/robertocoviello/), or at [roberto@blackwhale.dev](mailto:roberto@blackwhale.dev). Find all my for [here](https://bento.me/blackwhale) or join our growing XR developer community on [Discord](https://discord.gg/KkstGGwueN).

Acknowledgements
================

- **[Meta](https://developers.meta.com/horizon/documentation/unity/unity-development-overview/):** For the Passthrough Camera API.
- **[Unity Sentis](https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/index.html):** For powering the image tracking samples.
- **[OpenAI](https://platform.openai.com/docs/guides/vision):** For providing advanced vision capabilities.
- Thanks to shader wizard [Daniel Ilett](https://www.youtube.com/@danielilett) for helping me set up the `FrostedGlass` scene.
- Thanks to **[Michael Jahn](https://github.com/micjahn/ZXing.Net/)** for the XZing.Net library used for the QR code tracking samples.
- Thanks to **[Julian Triveri](https://github.com/trev3d/QuestDisplayAccessDemo)** for constantly pushing the boundaries with what is possible with Meta Quest hardware.

--------------------------------------------------------------------------------
Happy coding and enjoy exploring the possibilities with QuestVisionKit!
--------------------------------------------------------------------------------
