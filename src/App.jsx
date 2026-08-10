import React, { useState, useEffect } from 'react';
import { initializeApp } from 'firebase/app';
import { getAuth, onAuthStateChanged, signInWithEmailAndPassword, signOut, GoogleAuthProvider } from 'firebase/auth';
import {
  getFirestore,
  collection,
  addDoc,
  deleteDoc,
  doc,
  onSnapshot,
  updateDoc
} from 'firebase/firestore';
import LoginPage from './screens/LoginPage';
import AdminPage from './screens/AdminPage';

const firebaseConfig = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
  appId: import.meta.env.VITE_FIREBASE_APP_ID
};

// Inisialisasi Firebase (Tanpa Storage)
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);
const db = getFirestore(app);

function App() {

  const [authReady, setAuthReady] = useState(false);
  const [toasts, setToasts] = useState([]);
  const [user, setUser] = useState(null);

  const showToast = (message, type = 'info') => {
    const id = Date.now();
    setToasts(prev => [...prev, { id, message, type }]);
    setTimeout(() => {
      setToasts(prev => prev.filter(t => t.id !== id));
    }, 4000);
  };

  useEffect(() => {
    const unsubscribeAuth = onAuthStateChanged(auth, (currentUser) => {
      setUser(currentUser);
      setAuthReady(true);
    });
    return () => unsubscribeAuth();
  }, []);

  const handleLogout = async () => {
    try {
      await signOut(auth);
      showToast("Berhasil logout.", "info");
    } catch (error) {
      showToast("Logout gagal: " + error.message, "error");
    }
  };

  useEffect(() => {
    let intervalId;
    if (user) {
      if (!localStorage.getItem('loginTime')) {
        localStorage.setItem('loginTime', Date.now());
      }

      intervalId = setInterval(() => {
        const loginTime = localStorage.getItem('loginTime');
        const elapsedTime = Date.now() - parseInt(loginTime);
        const maxSession = 30 * 60 * 1000;

        if (elapsedTime >= maxSession) {
          handleLogout();
          localStorage.removeItem('loginTime');
          showToast("Sesi 30 menit telah berakhir demi keamanan. Silakan masuk ulang.", "error");
        }
      }, 60000);
    } else {
      localStorage.removeItem('loginTime');
    }

    return () => clearInterval(intervalId);
  }, [user]);

  if (!user) {
    return (
      <>
        <LoginPage auth={auth} showToast={showToast} />
        <div className="toast-container">
          {toasts.map(toast => (
            <div className={`toast ${toast.type}`} key={toast.id}>{toast.message}</div>
          ))}
        </div>
      </>
    );
  }

  return (
    <>
      <AdminPage db={db} handleLogout={handleLogout} showToast={showToast} />

      {/* Toast Notification Container */}
      <div className="toast-container">
        {toasts.map(toast => (
          <div className={`toast ${toast.type}`} key={toast.id}>
            {toast.message}
          </div>
        ))}
      </div>
    </>
  );

}

export default App;
