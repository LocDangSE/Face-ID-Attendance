import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout/Layout';
import Dashboard from './pages/Dashboard';
import Students from './pages/Students';
import Classes from './pages/Classes';
import Sessions from './pages/Sessions';
import TakeAttendance from './pages/TakeAttendance';

function App() {
    return (
        <Router>
            <Routes>
                <Route path="/" element={<Layout />}>
                    <Route index element={<Dashboard />} />
                    <Route path="students" element={<Students />} />
                    <Route path="classes" element={<Classes />} />
                    <Route path="sessions" element={<Sessions />} />
                    <Route path="take-attendance" element={<TakeAttendance />} />
                </Route>
            </Routes>
        </Router>
    );
}

export default App;
