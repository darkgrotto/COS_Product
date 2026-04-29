import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'
import 'keyrune/css/keyrune.css'
import { installCsrfFetch } from './lib/csrf'

// Patch window.fetch before any component mounts so all subsequent state-changing
// requests carry the X-CSRF-TOKEN header.
installCsrfFetch()

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
