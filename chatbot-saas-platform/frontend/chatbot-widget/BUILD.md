# Arif Chat Widget

A customizable chat widget that can be embedded into any website.

## Build Output

After running `npm run build`, you'll find the following files in the `dist/` directory:

### Widget Files
- `arif-chat-widget.es.js` - ES Module version (1.7MB)
- `arif-chat-widget.umd.js` - UMD version for browser globals (690KB)
- `arif-chat-widget.min.js` - IIFE version for immediate execution (690KB)
- `arif-widget.css` - Widget styles (12KB)
- `*.map` files - Source maps for debugging

### Demo & Assets
- `demo.html` - Demo page showing all integration methods
- `vite.svg` - Asset file

## Usage

### 1. UMD Version (Browser Global)

```html
<!DOCTYPE html>
<html>
<head>
    <link rel="stylesheet" href="./arif-widget.css">
</head>
<body>
    <script src="./arif-chat-widget.umd.js"></script>
    <script>
        const widget = new ArifChatWidget.ChatbotWidget();
        widget.init({
            tenantId: 'your-tenant-id',
            apiUrl: 'https://your-api.com',
            websocketUrl: 'wss://your-websocket.com/chatHub',
            theme: {
                primaryColor: '#007bff',
                position: 'bottom-right'
            },
            branding: {
                companyName: 'Your Company',
                welcomeMessage: 'Hello! How can I help you today?'
            }
        });
    </script>
</body>
</html>
```

### 2. ES Module Version (Modern Bundlers)

```javascript
import { ChatbotWidget } from './arif-chat-widget.es.js';

const widget = new ChatbotWidget();
widget.init({
    tenantId: 'your-tenant-id',
    apiUrl: 'https://your-api.com',
    websocketUrl: 'wss://your-websocket.com/chatHub'
});
```

### 3. Auto-initialization via Script Tag

```html
<script 
    src="./arif-chat-widget.umd.js" 
    data-chatbot-config='{"tenantId":"your-tenant-id","apiUrl":"https://your-api.com"}'>
</script>
<link rel="stylesheet" href="./arif-widget.css">
```

### 4. IIFE Version (Immediate Execution)

```html
<script src="./arif-chat-widget.min.js"></script>
<link rel="stylesheet" href="./arif-widget.css">
<script>
    const widget = new ArifChatWidget.ChatbotWidget();
    widget.init(config);
</script>
```

## Configuration Options

```typescript
interface WidgetConfig {
  tenantId: string;
  apiUrl?: string;
  websocketUrl?: string;
  authToken?: string;
  userId?: string;
  metadata?: Record<string, any>;
  theme?: {
    primaryColor?: string;
    secondaryColor?: string;
    backgroundColor?: string;
    textColor?: string;
    borderRadius?: string;
    fontFamily?: string;
    fontSize?: string;
    headerColor?: string;
    headerTextColor?: string;
    userMessageColor?: string;
    botMessageColor?: string;
    shadowColor?: string;
    position?: "bottom-right" | "bottom-left" | "top-right" | "top-left";
    size?: "small" | "medium" | "large";
    animation?: "slide" | "fade" | "bounce" | "none";
  };
  branding?: {
    logo?: string;
    companyName?: string;
    welcomeMessage?: string;
    placeholderText?: string;
  };
  language?: "en" | "ar";
  customCSS?: string;
  features?: {
    fileUpload?: boolean;
    voiceMessages?: boolean;
    typing?: boolean;
    readReceipts?: boolean;
    agentHandoff?: boolean;
    conversationRating?: boolean;
  };
}
```

## Widget Methods

```javascript
const widget = new ChatbotWidget();

// Initialize the widget
widget.init(config);

// Open/show the widget
widget.open();

// Close/hide the widget
widget.close();

// Destroy the widget
widget.destroy();

// Check if widget is ready
widget.isReady(); // returns boolean
```

## File Sizes

- **ES Module**: ~1.7MB (uncompressed), ~327KB (gzipped)
- **UMD/IIFE**: ~690KB (uncompressed), ~213KB (gzipped)
- **CSS**: ~12KB (uncompressed), ~2.6KB (gzipped)

## Testing

Open the `demo.html` file in your browser to test different integration methods.

## Development

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview build
npm run preview
```

## Browser Support

- Modern browsers (ES2015+)
- Chrome 51+
- Firefox 54+
- Safari 10+
- Edge 15+
