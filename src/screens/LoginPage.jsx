import React, { useState } from 'react';
import { signInWithEmailAndPassword } from 'firebase/auth';
import '../styles/LoginPage.css';

function LoginPage({ auth, showToast }) {
    const [loginEmail, setLoginEmail] = useState('');
    const [loginPassword, setLoginPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
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
                        <div className="password-input-wrapper">
                            <input
                                type={showPassword ? "text" : "password"}
                                className="input-pro"
                                placeholder="••••••••"
                                value={loginPassword}
                                onChange={(e) => setLoginPassword(e.target.value)}
                                required
                            />
                            <span
                                className="password-toggle-icon"
                                onClick={() => setShowPassword(!showPassword)}
                            >
                                {showPassword ? (
                                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#666" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                        <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                                        <line x1="1" y1="1" x2="23" y2="23"></line>
                                    </svg>
                                ) : (
                                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#666" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                                        <circle cx="12" cy="12" r="3"></circle>
                                    </svg>
                                )}
                            </span>
                        </div>
                    </div>

                    <div className="login-options-pro">
                        <label>
                            <input type="checkbox" /> Checkbox
                        </label>
                        <a href="#"><strike>Forgot Password?</strike></a>
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
