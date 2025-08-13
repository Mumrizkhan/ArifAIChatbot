import { Provider } from "react-redux";
import { store } from "./store/store";
import { ChatWidget } from "./components/ChatWidget";
import "./i18n";

function App() {
  return (
    <Provider store={store}>
      <div className="App">
        1111111111111111
        <ChatWidget />
      </div>
    </Provider>
  );
}

export default App;
