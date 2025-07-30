import { Provider } from 'react-redux';
import { store } from './store/store';
import { ChatWidget } from './components/ChatWidget';
import './i18n';

function App() {
  return (
    <Provider store={store}>
      <div className="App">
        <ChatWidget />
      </div>
    </Provider>
  );
}

export default App;
