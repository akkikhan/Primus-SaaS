# 🎙️ Voice Assistant Integration

A premium AI-powered voice overlay communicator for the Primus SaaS Portal with **OpenAI Whisper** speech-to-text and intelligent command processing.

---

## ✨ Features

### 🎯 Core Capabilities
- **Speech-to-Text**: OpenAI Whisper API integration with browser fallback
- **Text-to-Speech**: ElevenLabs premium voices with browser TTS fallback
- **Natural Language Commands**: Intelligent command processing with fuzzy matching
- **Real-time Transcription**: Live conversation display with timestamps
- **Voice Navigation**: Navigate the portal hands-free
- **Smart Responses**: Context-aware AI responses

### 🎨 Premium UI
- Floating microphone button with pulse animations
- Glassmorphic overlay panel
- Wave animations during listening
- Smooth transitions and micro-interactions
- Real-time visual feedback
- Dark mode optimized design

---

## 🚀 Quick Start

### 1. Install Dependencies
All required dependencies are already included in `package.json`. No additional installation needed!

### 2. Configure API Keys (Optional but Recommended)

Create a `.env` file in the `portal/frontend` directory:

```bash
# Required for premium Whisper transcription
VITE_OPENAI_API_KEY=sk-your-openai-api-key-here

# Optional: Premium voice responses (ElevenLabs)
VITE_ELEVENLABS_API_KEY=your-elevenlabs-api-key
VITE_ELEVENLABS_VOICE_ID=EXAVITQu4vr4xnSDxMaL
```

> **Note**: Without API keys, the assistant will use browser's built-in SpeechRecognition and SpeechSynthesis APIs (free but less accurate).

### 3. Get Your API Keys

#### OpenAI (Whisper)
1. Go to [platform.openai.com](https://platform.openai.com)
2. Create an account or sign in
3. Navigate to API Keys
4. Create new secret key
5. Copy and add to `.env` as `VITE_OPENAI_API_KEY`

#### ElevenLabs (Optional - Premium Voice)
1. Go to [elevenlabs.io](https://elevenlabs.io)
2. Create an account
3. Navigate to Profile → API Keys
4. Copy your API key
5. Add to `.env` as `VITE_ELEVENLABS_API_KEY`

### 4. Run the Application

```bash
npm run dev
```

The voice assistant will appear as a floating purple microphone button in the bottom-right corner!

---

## 🎮 Usage

### Activating the Assistant

1. Click the **floating microphone button** (bottom-right)
2. Click **"Start"** to begin listening
3. Speak your command clearly
4. Click **"Stop"** when finished
5. The assistant will transcribe, process, and respond

### Voice Commands

#### Navigation
- "Go to notifications"
- "Open dashboard"
- "Show settings"
- "Navigate to applications"

#### Actions
- "Send test notification"
- "Refresh page"
- "Clear screen"

#### Queries
- "What page am I on?"
- "What time is it?"
- "What can you do?"
- "Help"

#### General
- "Hello" / "Hi"
- "Thank you"
- "Goodbye"

The assistant uses **fuzzy matching**, so natural variations work too!

---

## 🏗️ Architecture

### Components

```
src/
├── components/
│   └── VoiceOverlay.tsx          # Main voice assistant UI
├── services/
│   ├── whisperService.ts         # OpenAI Whisper integration
│   ├── voiceResponseService.ts   # Text-to-Speech (ElevenLabs + Browser)
│   └── commandProcessor.ts       # Natural language command interpreter
└── hooks/
    └── useVoiceNavigation.ts     # React Router integration
```

### Service Architecture

```
User Speech
    ↓
MediaRecorder (Browser API)
    ↓
whisperService.transcribe()
    ↓ (OpenAI Whisper API)
Transcribed Text
    ↓
commandProcessor.process()
    ↓ (Pattern Matching + Fuzzy Logic)
Response + Action
    ↓
voiceResponseService.speak()
    ↓ (ElevenLabs or Browser TTS)
Audio Playback
```

---

## ⚙️ Configuration

### Whisper Service

```typescript
// Automatic language detection
formData.append('language', 'en'); // Remove for auto-detect

// Model selection
formData.append('model', 'whisper-1'); // Default model
```

### ElevenLabs Voice Settings

```typescript
voice_settings: {
    stability: 0.4,           // Lower = more emotional variance
    similarity_boost: 0.75,   // Voice consistency
    style: 0.15,             // Influencer energy
}
```

### Browser TTS Settings

```typescript
utterance.rate = 0.9;   // Speed (0.1 - 10)
utterance.pitch = 1.1;  // Pitch (0 - 2)
utterance.volume = 1.0; // Volume (0 - 1)
```

---

## 🎯 Extending the Assistant

### Adding New Commands

Edit `src/services/commandProcessor.ts`:

```typescript
// Add in process() method
if (this.matchesAny(lowerCommand, ['custom command', 'trigger words'])) {
    return 'Your response here';
}
```

### Adding Custom Actions

```typescript
// Trigger custom event
this.executeAction('action-id', 'Performing action...');

// Listen in component
useEffect(() => {
    const handler = (event: CustomEvent) => {
        if (event.detail.actionId === 'action-id') {
            // Your action logic
        }
    };
    window.addEventListener('voice-action', handler as EventListener);
    return () => window.removeEventListener('voice-action', handler as EventListener);
}, []);
```

### Changing Voice Model

For ElevenLabs, browse voices at [elevenlabs.io/voice-library](https://elevenlabs.io/voice-library) and update:

```bash
VITE_ELEVENLABS_VOICE_ID=<new-voice-id>
```

---

## 🔐 Security & Privacy

### Best Practices
- ✅ API keys are environment variables (not committed to git)
- ✅ Audio is processed in real-time (not stored)
- ✅ Microphone access requires user permission
- ✅ All API calls use HTTPS

### Data Flow
1. Audio captured locally in browser
2. Sent to OpenAI Whisper API (if configured)
3. Transcription returned as text
4. Processed locally by command processor
5. No audio data is stored on any server

---

## 🐛 Troubleshooting

### "Microphone access denied"
**Solution**: Grant microphone permissions in browser settings

### "Speech recognition not supported"
**Solution**: Add OpenAI API key or use Chrome/Edge browser

### "API error" or rate limits
**Solution**: Check API key validity and billing status

### No audio response
**Solution**: 
- Check if muted (speaker icon)
- Verify ElevenLabs API key
- Check browser volume settings

### Commands not recognized
**Solution**: 
- Speak clearly and naturally
- Try alternative phrasings
- Say "help" to see available commands

---

## 📊 Performance

### API Response Times
- **Whisper Transcription**: ~1-3 seconds
- **Command Processing**: <100ms (local)
- **ElevenLabs TTS**: ~0.5-2 seconds
- **Browser TTS**: <500ms (instant)

### Optimization Tips
- Use browser APIs for low-latency (set API keys to empty)
- WebM audio format is optimal for Whisper
- Consider caching common responses

---

## 🎨 Customization

### Styling
All styles are in the component using Tailwind CSS classes. Key design tokens:

```css
Colors:
- Primary: purple-600 to blue-600 gradient
- Active: red-500 to pink-500 gradient
- Background: gray-900 with glassmorphic blur
- Text: white/gray-300

Animations:
- Pulse ring on listening state
- Wave animation (3 bars)
- Slide-in transitions
- Fade effects
```

### Position
Change the floating button position in `VoiceOverlay.tsx`:

```tsx
className="fixed bottom-6 right-6"
// Change to: top-6, left-6, etc.
```

---

## 🔮 Future Enhancements

- [ ] Multi-language support
- [ ] Wake word detection ("Hey Primus")
- [ ] Continuous listening mode
- [ ] Voice biometrics/authentication
- [ ] Custom voice training
- [ ] Conversation history export
- [ ] Integration with GPT-4 for advanced AI
- [ ] Context awareness (current page data)

---

## 📚 References

- [OpenAI Whisper Docs](https://platform.openai.com/docs/guides/speech-to-text)
- [ElevenLabs API Docs](https://elevenlabs.io/docs/api-reference/text-to-speech)
- [Web Speech API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Speech_API)
- [MediaRecorder API](https://developer.mozilla.org/en-US/docs/Web/API/MediaRecorder)

---

## 💡 Tips for Best Results

### For OpenAI Whisper
- Speak clearly with minimal background noise
- Use a quality microphone
- Keep commands concise (under 10 seconds)

### For ElevenLabs
- Choose voices that match your brand
- Adjust stability for emotion (lower = more human-like)
- Use SSML tags in responses for pauses/emphasis

### For Browser APIs
- Use Chrome or Edge for best compatibility
- Enable microphone in site settings
- Reduce background noise for accuracy

---

## 🤝 Contributing

Want to improve the voice assistant? Here's how:

1. Add new command patterns to `commandProcessor.ts`
2. Enhance fuzzy matching algorithm
3. Add support for more languages
4. Improve error handling
5. Create custom voice personas

---

## 📄 License

Part of the Primus SaaS Portal project.

---

**Built with ❤️ for the Primus SaaS Platform**

*Making enterprise software conversational, one command at a time.*
