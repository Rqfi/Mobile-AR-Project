import React, { useState } from 'react';
import { signInWithEmailAndPassword } from 'firebase/auth';
import '../styles/LoginPage.css';

function LoginPage({ auth, showToast }) {
    const [loginEmail, setLoginEmail] = useState('');
    const [loginPassword, setLoginPassword] = useState('');
    const [loading, setLoading] = useState(false);

    const handleLogin = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            await signInWithEmailAndPassword(auth, loginEmail, loginPassword);
            showToast("Berhasil login sebagai admin!", "success");
        } catch (error) {
            console.error("Login gagal:", error);
            showToast("Login gagal: Periksa kembali Email/Password Anda.", "error");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="login-page-wrapper">
            <div className="login-card-pro">

                <div className="login-header-pro">
                    <h1>HOMEI</h1>
                    <h2>Admin Console</h2>
                    <p>Welcome! Please enter your credentials.</p>
                </div>

                <form onSubmit={handleLogin}>
                    <div className="form-group-pro">
                        <label>Email</label>
                        <input
                            type="email"
                            className="input-pro"
                            placeholder="admin@homei.com"
                            value={loginEmail}
                            onChange={(e) => setLoginEmail(e.target.value)}
                            required
                        />
                    </div>

                    <div className="form-group-pro">
                        <label>Password</label>
                        <input
                            type="password"
                            className="input-pro"
                            placeholder="••••••••"
                            value={loginPassword}
                            onChange={(e) => setLoginPassword(e.target.value)}
                            required
                        />
                    </div>

                    <div className="login-options-pro">
                        <label>
                            <input type="checkbox" /> Remember me
                        </label>
                        <a href="#">Forgot Password?</a>
                    </div>

                    <button type="submit" className="btn-pro" disabled={loading}>
                        {loading ? "Memproses..." : "Masuk ke Konsol"}
                    </button>
                </form>

                <div className="back-link-pro">
                    {/* <a href="#">&larr; Back to Website</a> */}
                </div>

            </div>
        </div>
    );
}

export default LoginPage;
