import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Package, Lock, User, ArrowRight, ShieldCheck } from "lucide-react";
import toast from "react-hot-toast";

const SupplierLogin: React.FC = () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      // For demo, we'll use a hardcoded login if API fails or for speed
      // In production, this would call /api/supplier-portal/login
      const response = await fetch("http://localhost:5100/api/supplier-portal/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      const result = await response.json();
      if (result.success) {
        localStorage.setItem("supplier_token", result.data.token);
        localStorage.setItem("supplier_id", result.data.supplierId);
        localStorage.setItem("supplier_name", result.data.supplierName);
        toast.success(`歡迎回來, ${result.data.supplierName}`);
        navigate("/supplier/rfqs");
      } else {
        toast.error(result.message || "登入失敗");
      }
    } catch (error) {
      console.error("Login error:", error);
      // Fallback for development if backend not seeded
      if (username === "supplier" && password === "partner123") {
        localStorage.setItem("supplier_token", "mock_supplier_token");
        localStorage.setItem("supplier_id", "00000000-0000-0000-0000-000000000001");
        localStorage.setItem("supplier_name", "示範供應商");
        toast.success("開發模式登入成功");
        navigate("/supplier/rfqs");
      } else {
        toast.error("服務連線失敗");
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#04080f] flex items-center justify-center p-6 font-['Inter',_sans-serif] relative overflow-hidden">
      {/* Background Decorative Elements */}
      <div className="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] bg-blue-600/10 blur-[120px] rounded-full"></div>
      <div className="absolute bottom-[-10%] right-[-10%] w-[40%] h-[40%] bg-purple-600/10 blur-[120px] rounded-full"></div>

      <div className="w-full max-w-md relative z-10">
        <div className="text-center mb-10">
          <div className="w-20 h-20 bg-gradient-to-br from-blue-600 to-blue-400 rounded-2xl flex items-center justify-center mx-auto mb-6 shadow-2xl shadow-blue-600/20 rotate-3 transform hover:rotate-0 transition-transform duration-500">
            <Package size={40} className="text-white" />
          </div>
          <h1 className="text-3xl font-extrabold text-white tracking-tight mb-2">供應商協作入口</h1>
          <p className="text-gray-400">Supplier Collaboration Portal</p>
        </div>

        <div className="bg-[#0d1829]/80 backdrop-blur-xl border border-[#1a3050] rounded-3xl p-8 shadow-2xl">
          <form onSubmit={handleLogin} className="space-y-6">
            <div className="space-y-2">
              <label className="text-sm font-medium text-gray-400 ml-1">帳號</label>
              <div className="relative group">
                <User className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-500 group-focus-within:text-blue-500 transition-colors" size={20} />
                <input
                  type="text"
                  required
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  className="w-full bg-[#1a3050]/50 border border-[#1a3050] focus:border-blue-500 rounded-2xl py-4 pl-12 pr-4 text-white placeholder-gray-500 outline-none transition-all focus:ring-4 focus:ring-blue-500/10"
                  placeholder="請輸入供應商帳號"
                />
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium text-gray-400 ml-1">密碼</label>
              <div className="relative group">
                <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-500 group-focus-within:text-blue-500 transition-colors" size={20} />
                <input
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full bg-[#1a3050]/50 border border-[#1a3050] focus:border-blue-500 rounded-2xl py-4 pl-12 pr-4 text-white placeholder-gray-500 outline-none transition-all focus:ring-4 focus:ring-blue-500/10"
                  placeholder="請輸入安全密碼"
                />
              </div>
            </div>

            <div className="flex items-center justify-between text-sm">
              <label className="flex items-center gap-2 cursor-pointer group">
                <input type="checkbox" className="w-4 h-4 rounded border-[#1a3050] bg-[#1a3050] text-blue-600 focus:ring-0 focus:ring-offset-0" />
                <span className="text-gray-400 group-hover:text-gray-300 transition-colors">記住帳號</span>
              </label>
              <a href="#" className="text-blue-400 hover:text-blue-300 transition-colors font-medium">忘記密碼？</a>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-gradient-to-r from-blue-600 to-blue-500 hover:from-blue-500 hover:to-blue-400 text-white font-bold py-4 rounded-2xl shadow-lg shadow-blue-600/20 transition-all flex items-center justify-center gap-2 group disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? (
                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
              ) : (
                <>
                  登入入口 <ArrowRight size={20} className="group-hover:translate-x-1 transition-transform" />
                </>
              )}
            </button>
          </form>

          <div className="mt-8 pt-8 border-t border-[#1a3050] flex items-center justify-center gap-6">
            <div className="flex items-center gap-2 text-xs text-gray-500">
              <ShieldCheck size={14} className="text-green-500" />
              <span>SSL 安全加密</span>
            </div>
            <div className="w-px h-3 bg-[#1a3050]"></div>
            <div className="text-xs text-gray-500">
              Ver. 2.0.4-LTS
            </div>
          </div>
        </div>

        <p className="text-center mt-8 text-gray-500 text-sm">
          還沒有帳號？ <a href="#" className="text-blue-400 hover:underline">聯繫採購部門</a>
        </p>
      </div>
    </div>
  );
};

export default SupplierLogin;
