# SharpMonoInjector TheHolyOneZ Edition v2.5

A complete visual overhaul of SharpMonoInjector with a modern dark theme, enhanced user experience, and advanced stealth injection capabilities.

![SharpMonoInjector GUI](Images/NewScreen1.png)

## 🆕 What's New in v2.5 (Latest Update)

### 🥷 Stealth Injection System

**NEW**: Advanced anti-detection features for bypassing monitoring and anti-cheat systems.

#### Enable Stealth Mode Checkbox
- One-click toggle for all stealth features
- Visual indicator in status bar when stealth is active
- Shows "Injection successful (STEALTH MODE)" on completion

#### Stealth Features

**1. Memory Randomization**
- Adds 4-64 random NOP instructions before shellcode execution
- Makes memory patterns unpredictable and harder to signature-scan
- Randomizes on every injection for maximum variance

**2. Thread Hiding**
- Creates threads with `CREATE_SUSPENDED` flag
- Uses `NtSetInformationThread` to hide from thread enumeration tools
- Properly resumes threads after hiding to prevent freezing
- Evades basic thread scanning tools

**3. Execution Delay**
- 150ms delay before injection starts
- Helps evade time-based behavioral analysis
- Configurable delay timing in code

**4. Debugger Detection**
- Checks if target process has a debugger attached
- Displays warning: "WARNING: Target process is being debugged!"
- Helps avoid detection by runtime debugging tools

**5. Code Obfuscation** *(Experimental - Currently Disabled)*
- XOR encryption of shellcode with random key
- Dynamic decoder stub generated at runtime
- Disabled by default for stability

### 🐛 Bug Fixes in v2.5

**Process Selection Fix**
- Fixed ComboBox not displaying selected Mono process
- Resolved `INotifyPropertyChanged` binding issue in `SelectedProcess` property
- Process names now properly display after refresh

**Thread Management - Development Phase**
- Fixed application freezing when using stealth mode
- Added `ResumeThread` call after thread hiding
- Proper thread lifecycle management

### 🔧 Technical Additions

**New Files Modified:**
- `Injector.cs` - Complete stealth system implementation with `InjectionOptions` class
- `Native.cs` - Added `CheckRemoteDebuggerPresent`, `NtSetInformationThread`, `ResumeThread` P/Invoke methods
- `Assembler.cs` - Added `Nop()` method for memory randomization
- `MainWindowViewModel.cs` - Added `UseStealthMode` property and stealth configuration
- `MainWindow.xaml` - Added stealth mode checkbox with modern styling

**New Classes:**
- `InjectionOptions` - Configuration class for stealth features with boolean flags for each feature

### 📊 Performance Impact

| Mode | Injection Time | Detection Risk |
|------|---------------|----------------|
| Normal | ~50-100ms | Higher |
| Stealth | ~250-350ms | Significantly Lower |

Worth the extra 200ms for stealth operations!

## What's New in TheHolyOneZ Edition (Design) v2.5

### 🎨 Design Enhancements

- **Modern Dark Theme**: Sleek dark interface with vibrant `#00E676` green accents for a professional hacker/developer aesthetic
- **Rounded Corners**: All UI elements feature subtle rounded corners (4-8px) for a contemporary look
- **Premium Typography**: Segoe UI font family throughout with proper weights and sizing hierarchy
- **Smooth Interactions**: All interactive elements include polished hover states and transitions
- **Optimized Layout**: Improved spacing, padding, and visual hierarchy across the entire interface

### 🔧 Fixed Issues (v2.5)

- **Scrollbar Styling**: Custom-styled scrollbars with green theme that match the overall design
- **Overflow Protection**: Fixed injected assemblies overflow with proper ScrollViewer implementation and fixed 120px height
- **Button Sizing**: Increased button sizes (MinWidth: 90px) for better usability and touch-friendly interactions
- **Scrollbar Overlap**: Eliminated scrollbar overlap with content through proper margin management
- **Content Scrolling**: Added ScrollViewers to both main sections for graceful overflow handling

### ⚡ Technical Improvements (v2.5)

- **Updated Framework**: Upgraded from .NET Framework 4.0 to 4.8 for improved performance and modern compatibility

### ✨ Key Visual Features


#### Card-Based Layout
- Both Inject and Eject sections housed in elevated card-style containers
- `#FF1E1E1E` background with `8px` rounded corners
- Clear visual separation between functional areas

#### Modern Button Styles
- **Primary Actions** (INJECT/EJECT): Solid green background with black text
- **Secondary Actions** (Refresh/Browse): Outlined style that fills green on hover
- All buttons feature smooth color transitions and hand cursor

#### Enhanced Input Fields
- Dark themed text boxes and combo boxes (`#FF252525`)
- Green border highlights on hover and focus states
- Consistent padding and rounded corners across all inputs

#### Status Bar
- Sleek black footer with real-time status updates
- Custom "Design by TheHolyOneZ" signature with clickable GitHub link
- Clean typography and proper text trimming for long messages

#### List Improvements
- Styled ListBox with custom scrollbars
- Selected items display with full green background and black text for optimal contrast
- Hover states on list items for better user feedback

## About SharpMonoInjector

SharpMonoInjector is a tool for injecting assemblies into Mono embedded applications, commonly Unity Engine based games. The target process *usually* does not have to be restarted in order to inject an updated version of the assembly. Your unload method must destroy all of its resources (such as game objects).

SharpMonoInjector works by dynamically generating machine code, writing it to the target process and executing it using CreateRemoteThread. The code calls functions in the mono embedded API. The return value is obtained with ReadProcessMemory.

**Both x86 and x64 processes are supported.**

## Usage Requirements

### Method Signature
In order for the injector to work, the load/unload methods need to match the following signature:

```csharp
static void Method()
```

Note: Unload method is optional but recommended for clean resource cleanup.

### Stealth Mode Usage
1. Select your target Mono process from the dropdown
2. Browse and select your DLL assembly
3. Fill in Namespace, Class name, and Method name
4. **Check "Enable Stealth Mode"** for anti-detection features
5. Click INJECT
6. Status bar will show "(STEALTH MODE)" if successful

### Administrator Privileges
The GUI version will automatically request and restart with Administrator privileges when needed. For the console version, you'll receive a warning with instructions if privileges are insufficient.

## Anti-Detection Capabilities

When **Stealth Mode** is enabled:

✅ **Evades Static Signatures** - Random NOP padding breaks signature patterns  
✅ **Evades Memory Scanners** - Randomized shellcode layout per injection  
✅ **Evades Thread Enumeration** - Hidden threads via `NtSetInformationThread`  
✅ **Evades Timing Analysis** - Execution delay disrupts time-based detection  
✅ **Debugger Awareness** - Warns when target has active debugger

### Recommended Use Cases for Stealth Mode
- Games with anti-cheat systems
- Applications with integrity monitoring
- Processes with active security scanning
- Any environment requiring low-profile injection

## Features from wh0am1 Mod

This edition builds upon the wh0am1 mod which fixed:
- Process detection bugs
- x86/x64 detection issues
- Added privilege checking and auto-elevation
- Enhanced error handling
- Built on .NET 4.0 for Windows 7+ compatibility

## Version History

### v2.5 (TheHolyOneZ Edition)
- Added complete stealth injection system
- Fixed ComboBox process display bug
- Fixed thread freezing in stealth mode
- Added debugger detection
- Enhanced anti-detection capabilities

- Complete visual overhaul with modern dark theme
- Custom styled scrollbars and UI components
- Upgraded to .NET Framework 4.8
- Improved layout and UX

### Original Versions
- wh0am1 mod improvements
- Warbler's original implementation

## Credits

- **TheHolyOneZ** - Visual overhaul, UI/UX improvements, and stealth injection system
- **wh0am1** - Bug fixes and stability improvements ([Original Mod](https://www.unknowncheats.me/forum/unity/408878-sharpmonoinjector-fixed-updated.html))
- **Warbler** - Original SharpMonoInjector creator ([Original Project](https://github.com/warbler/SharpMonoInjector))


The developers are not responsible for misuse of this software.

## Disclaimer (Cough Cough)

This tool is for educational and legitimate testing purposes only. The stealth features are designed for:
- Legitimate mod development and testing
- Security research in controlled environments
- Personal single-player game modifications

**Do not use this tool to:**
- Cheat in online multiplayer games
- Bypass security in production applications
- Violate any terms of service or end-user agreements
- Engage in any illegal or unethical activities

## License

This project maintains the same license as the original SharpMonoInjector project.

---

**Design/Modifications by [TheHolyOneZ](https://github.com/TheHolyOneZ)**

*README Crafted with assistance from Claude (Anthropic)*
