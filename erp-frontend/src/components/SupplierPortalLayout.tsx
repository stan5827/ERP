import React from "react";
import { Outlet, useNavigate, Link } from "react-router-dom";
import { Package, FileText, Truck, BarChart3, LogOut, User, Menu, X } from "lucide-react";

const SupplierPortalLayout: React.FC = () => {
  const [isSidebarOpen, setIsSidebarOpen] = React.useState(true);
  const navigate = useNavigate();
  const supplierName = localStorage.getItem("supplier_name") || "供應商夥伴";

  const handleLogout = () => {
    localStorage.removeItem("supplier_token");
    localStorage.removeItem("supplier_id");
    localStorage.removeItem("supplier_name");
    navigate("/supplier/login");
  };

  const navItems = [
    { icon: <FileText size={20} />, label: "詢價單 (RFQs)", path: "/supplier/rfqs" },
    { icon: <Package size={20} />, label: "採購訂單 (POs)", path: "/supplier/purchase-orders" },
    { icon: <Truck size={20} />, label: "發貨通知 (ASN)", path: "/supplier/asns" },
    { icon: <BarChart3 size={20} />, label: "績效評分", path: "/supplier/performance" },
  ];

  return (
    <div className="min-h-screen bg-[#04080f] text-[#e8eef8] flex font-['Inter',_sans-serif]">
      {/* Sidebar */}
      <aside 
        className={`${isSidebarOpen ? 'w-64' : 'w-20'} bg-[#0d1829] border-r border-[#1a3050] transition-all duration-300 flex flex-col fixed inset-y-0 z-50`}
      >
        <div className="p-6 flex items-center justify-between border-b border-[#1a3050]">
          <div className={`flex items-center gap-3 ${!isSidebarOpen && 'hidden'}`}>
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
              <Package size={18} className="text-white" />
            </div>
            <span className="font-bold text-lg tracking-tight">Supplier Portal</span>
          </div>
          <button 
            onClick={() => setIsSidebarOpen(!isSidebarOpen)}
            className="p-1 hover:bg-[#1a3050] rounded transition-colors"
          >
            {isSidebarOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>

        <nav className="flex-1 py-6 px-4 space-y-2 overflow-y-auto">
          {navItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className="flex items-center gap-3 p-3 rounded-xl hover:bg-[#1a3050] transition-all group relative"
            >
              <span className="text-blue-400 group-hover:scale-110 transition-transform">
                {item.icon}
              </span>
              <span className={`font-medium transition-opacity duration-300 ${!isSidebarOpen && 'opacity-0 invisible'}`}>
                {item.label}
              </span>
              {!isSidebarOpen && (
                <div className="absolute left-16 bg-[#1a3050] px-2 py-1 rounded text-sm whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity z-50 pointer-events-none">
                  {item.label}
                </div>
              )}
            </Link>
          ))}
        </nav>

        <div className="p-4 border-t border-[#1a3050]">
          <div className={`flex items-center gap-3 mb-4 p-2 ${!isSidebarOpen && 'justify-center'}`}>
            <div className="w-10 h-10 rounded-full bg-blue-500/20 flex items-center justify-center border border-blue-500/50">
              <User size={20} className="text-blue-400" />
            </div>
            <div className={`${!isSidebarOpen && 'hidden'}`}>
              <p className="text-sm font-semibold truncate max-w-[140px]">{supplierName}</p>
              <p className="text-xs text-gray-400 uppercase tracking-widest">Partner</p>
            </div>
          </div>
          <button 
            onClick={handleLogout}
            className="w-full flex items-center gap-3 p-3 rounded-xl text-red-400 hover:bg-red-500/10 transition-all group"
          >
            <LogOut size={20} />
            <span className={`${!isSidebarOpen && 'hidden'} font-medium`}>安全退出</span>
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main className={`flex-1 transition-all duration-300 ${isSidebarOpen ? 'ml-64' : 'ml-20'} p-8`}>
        <div className="max-w-7xl mx-auto">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default SupplierPortalLayout;
