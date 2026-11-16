import React, { useEffect, useRef, useState } from 'react';
import * as THREE from 'three';

interface WarehouseZone {
  id: string;
  name: string;
  type: 'raw_material' | 'finished_goods' | 'production' | 'shipping';
  position: { x: number; y: number; z: number };
  size: { width: number; height: number; depth: number };
  utilization: number;
  status: 'normal' | 'warning' | 'critical';
  currentStock: number;
  maxCapacity: number;
}

interface Warehouse3DViewerProps {
  warehouseId?: string;
  onZoneClick?: (zone: WarehouseZone) => void;
}

const defaultZones: WarehouseZone[] = [
  { id: 'zone-1', name: '原料倉 A區', type: 'raw_material', position: { x: -10, y: 0, z: -5 }, size: { width: 8, height: 6, depth: 10 }, utilization: 0.75, status: 'normal', currentStock: 1500, maxCapacity: 2000 },
  { id: 'zone-2', name: '原料倉 B區', type: 'raw_material', position: { x: -10, y: 0, z: 5 }, size: { width: 8, height: 6, depth: 10 }, utilization: 0.45, status: 'normal', currentStock: 900, maxCapacity: 2000 },
  { id: 'zone-3', name: '成品倉 A區', type: 'finished_goods', position: { x: 10, y: 0, z: -5 }, size: { width: 8, height: 6, depth: 10 }, utilization: 0.90, status: 'warning', currentStock: 1800, maxCapacity: 2000 },
  { id: 'zone-4', name: '成品倉 B區', type: 'finished_goods', position: { x: 10, y: 0, z: 5 }, size: { width: 8, height: 6, depth: 10 }, utilization: 0.30, status: 'normal', currentStock: 600, maxCapacity: 2000 },
  { id: 'zone-5', name: '線邊倉', type: 'production', position: { x: 0, y: 0, z: 0 }, size: { width: 6, height: 4, depth: 6 }, utilization: 0.65, status: 'normal', currentStock: 780, maxCapacity: 1200 },
  { id: 'zone-6', name: '出貨區', type: 'shipping', position: { x: 18, y: 0, z: 0 }, size: { width: 5, height: 3, depth: 15 }, utilization: 0.20, status: 'normal', currentStock: 200, maxCapacity: 1000 },
];

export function Warehouse3DViewer({ onZoneClick }: Warehouse3DViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [zones] = useState<WarehouseZone[]>(defaultZones);
  const [selectedZone, setSelectedZone] = useState<WarehouseZone | null>(null);
  const [cameraPosition, setCameraPosition] = useState({ x: 30, y: 20, z: 30 });
  const sceneRef = useRef<THREE.Scene | null>(null);
  const rendererRef = useRef<THREE.WebGLRenderer | null>(null);
  const cameraRef = useRef<THREE.PerspectiveCamera | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    const container = containerRef.current;
    const width = container.clientWidth;
    const height = container.clientHeight;

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x1a1a2e);
    scene.fog = new THREE.Fog(0x1a1a2e, 50, 100);

    const camera = new THREE.PerspectiveCamera(60, width / height, 0.1, 1000);
    camera.position.set(cameraPosition.x, cameraPosition.y, cameraPosition.z);
    camera.lookAt(0, 0, 0);

    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(width, height);
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.shadowMap.enabled = true;
    container.appendChild(renderer.domElement);

    const ambientLight = new THREE.AmbientLight(0xffffff, 0.4);
    scene.add(ambientLight);

    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directionalLight.position.set(20, 30, 20);
    directionalLight.castShadow = true;
    scene.add(directionalLight);

    const gridHelper = new THREE.GridHelper(60, 30, 0x444444, 0x222222);
    scene.add(gridHelper);

    const floorGeometry = new THREE.PlaneGeometry(60, 40);
    const floorMaterial = new THREE.MeshStandardMaterial({ 
      color: 0x2a2a3e, 
      roughness: 0.9,
      metalness: 0.1 
    });
    const floor = new THREE.Mesh(floorGeometry, floorMaterial);
    floor.rotation.x = -Math.PI / 2;
    floor.position.y = -0.1;
    floor.receiveShadow = true;
    scene.add(floor);

    const zoneMeshes: THREE.Mesh[] = [];

    zones.forEach(zone => {
      const { width: w, height: h, depth: d } = zone.size;
      const geometry = new THREE.BoxGeometry(w, h, d);
      
      let color: number;
      switch (zone.status) {
        case 'critical': color = 0xff4444; break;
        case 'warning': color = 0xffaa00; break;
        default: color = 0x4a90d9;
      }

      const material = new THREE.MeshStandardMaterial({
        color,
        transparent: true,
        opacity: 0.7,
        roughness: 0.3,
        metalness: 0.5
      });

      const mesh = new THREE.Mesh(geometry, material);
      mesh.position.set(zone.position.x, h / 2, zone.position.z);
      mesh.castShadow = true;
      mesh.receiveShadow = true;
      mesh.userData = { zone };
      
      scene.add(mesh);
      zoneMeshes.push(mesh);

      const edgeGeometry = new THREE.EdgesGeometry(geometry);
      const edgeMaterial = new THREE.LineBasicMaterial({ color: 0xffffff, opacity: 0.5, transparent: true });
      const edges = new THREE.LineSegments(edgeGeometry, edgeMaterial);
      edges.position.copy(mesh.position);
      scene.add(edges);

      const stockLevel = zone.utilization;
      const stockHeight = h * stockLevel;
      const stockGeometry = new THREE.BoxGeometry(w * 0.8, stockHeight, d * 0.8);
      const stockMaterial = new THREE.MeshStandardMaterial({
        color: 0x2ecc71,
        transparent: true,
        opacity: 0.6
      });
      const stockMesh = new THREE.Mesh(stockGeometry, stockMaterial);
      stockMesh.position.set(zone.position.x, stockHeight / 2, zone.position.z);
      scene.add(stockMesh);
    });

    const labelSprites: THREE.Sprite[] = [];
    zones.forEach(zone => {
      const canvas = document.createElement('canvas');
      canvas.width = 256;
      canvas.height = 128;
      const ctx = canvas.getContext('2d')!;
      
      ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
      ctx.fillRect(0, 0, 256, 128);
      
      ctx.fillStyle = 'white';
      ctx.font = 'bold 24px Arial';
      ctx.textAlign = 'center';
      ctx.fillText(zone.name, 128, 40);
      
      ctx.font = '20px Arial';
      ctx.fillText(`${Math.round(zone.utilization * 100)}%`, 128, 70);
      ctx.fillText(`${zone.currentStock} / ${zone.maxCapacity}`, 128, 100);

      const texture = new THREE.CanvasTexture(canvas);
      const spriteMaterial = new THREE.SpriteMaterial({ map: texture });
      const sprite = new THREE.Sprite(spriteMaterial);
      sprite.position.set(zone.position.x, zone.size.height + 2, zone.position.z);
      sprite.scale.set(4, 2, 1);
      scene.add(sprite);
      labelSprites.push(sprite);
    });

    const raycaster = new THREE.Raycaster();
    const mouse = new THREE.Vector2();

    const onMouseMove = (event: MouseEvent) => {
      const rect = container.getBoundingClientRect();
      mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
      mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

      raycaster.setFromCamera(mouse, camera);
      const intersects = raycaster.intersectObjects(zoneMeshes);

      document.body.style.cursor = intersects.length > 0 ? 'pointer' : 'default';
    };

    const onClick = (event: MouseEvent) => {
      const rect = container.getBoundingClientRect();
      mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
      mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

      raycaster.setFromCamera(mouse, camera);
      const intersects = raycaster.intersectObjects(zoneMeshes);

      if (intersects.length > 0) {
        const zone = intersects[0].object.userData.zone as WarehouseZone;
        setSelectedZone(zone);
        onZoneClick?.(zone);
      } else {
        setSelectedZone(null);
      }
    };

    container.addEventListener('mousemove', onMouseMove);
    container.addEventListener('click', onClick);

    const animate = () => {
      requestAnimationFrame(animate);
      renderer.render(scene, camera);
    };
    animate();

    sceneRef.current = scene;
    rendererRef.current = renderer;
    cameraRef.current = camera;

    const handleResize = () => {
      const w = container.clientWidth;
      const h = container.clientHeight;
      camera.aspect = w / h;
      camera.updateProjectionMatrix();
      renderer.setSize(w, h);
    };

    window.addEventListener('resize', handleResize);

    return () => {
      window.removeEventListener('resize', handleResize);
      container.removeEventListener('mousemove', onMouseMove);
      container.removeEventListener('click', onClick);
      container.removeChild(renderer.domElement);
      renderer.dispose();
    };
  }, [zones, cameraPosition, onZoneClick]);

  const handleRotate = (direction: 'left' | 'right' | 'up' | 'down') => {
    const angle = 10 * (Math.PI / 180);
    setCameraPosition(prev => {
      const { x, y, z } = prev;
      switch (direction) {
        case 'left': return { x: x * Math.cos(angle) + z * Math.sin(angle), y, z: -x * Math.sin(angle) + z * Math.cos(angle) };
        case 'right': return { x: x * Math.cos(-angle) + z * Math.sin(-angle), y, z: -x * Math.sin(-angle) + z * Math.cos(-angle) };
        case 'up': return { x, y: y + 5, z };
        case 'down': return { x, y: Math.max(5, y - 5), z };
        default: return prev;
      }
    });
  };

  const handleZoom = (direction: 'in' | 'out') => {
    setCameraPosition(prev => {
      const scale = direction === 'in' ? 0.9 : 1.1;
      return { x: prev.x * scale, y: prev.y * scale, z: prev.z * scale };
    });
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'critical': return '#ff4444';
      case 'warning': return '#ffaa00';
      default: return '#4a90d9';
    }
  };

  return (
    <div style={{ display: 'flex', gap: 16, height: '100%' }}>
      <div style={{ flex: 1, position: 'relative' }}>
        <div ref={containerRef} style={{ width: '100%', height: '100%', minHeight: 500, borderRadius: 8, overflow: 'hidden' }} />
        
        <div style={{ position: 'absolute', bottom: 16, left: 16, display: 'flex', gap: 8 }}>
          <button className="btn btn-secondary" onClick={() => handleRotate('left')}>↶</button>
          <button className="btn btn-secondary" onClick={() => handleRotate('right')}>↷</button>
          <button className="btn btn-secondary" onClick={() => handleRotate('up')}>▲</button>
          <button className="btn btn-secondary" onClick={() => handleRotate('down')}>▼</button>
          <button className="btn btn-secondary" onClick={() => handleZoom('in')}>+</button>
          <button className="btn btn-secondary" onClick={() => handleZoom('out')}>-</button>
        </div>

        <div style={{ position: 'absolute', top: 16, left: 16, display: 'flex', gap: 16 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <div style={{ width: 16, height: 16, background: '#4a90d9', borderRadius: 4 }} />
            <span style={{ color: '#fff', fontSize: 12 }}>正常</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <div style={{ width: 16, height: 16, background: '#ffaa00', borderRadius: 4 }} />
            <span style={{ color: '#fff', fontSize: 12 }}>警告</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <div style={{ width: 16, height: 16, background: '#ff4444', borderRadius: 4 }} />
            <span style={{ color: '#fff', fontSize: 12 }}>危急</span>
          </div>
        </div>
      </div>

      {selectedZone && (
        <div style={{ width: 280, padding: 16, background: '#1e1e32', borderRadius: 8 }}>
          <h4 style={{ marginBottom: 16, color: '#fff' }}>{selectedZone.name}</h4>
          
          <div style={{ marginBottom: 12 }}>
            <div style={{ color: '#888', fontSize: 12, marginBottom: 4 }}>狀態</div>
            <div style={{ 
              display: 'inline-block', 
              padding: '4px 8px', 
              borderRadius: 4, 
              background: getStatusColor(selectedZone.status),
              color: '#fff',
              fontSize: 12
            }}>
              {selectedZone.status === 'normal' ? '正常' : selectedZone.status === 'warning' ? '警告' : '危急'}
            </div>
          </div>

          <div style={{ marginBottom: 12 }}>
            <div style={{ color: '#888', fontSize: 12, marginBottom: 4 }}>類型</div>
            <div style={{ color: '#fff' }}>
              {selectedZone.type === 'raw_material' ? '原料倉' : 
               selectedZone.type === 'finished_goods' ? '成品倉' : 
               selectedZone.type === 'production' ? '生產線邊倉' : '出貨區'}
            </div>
          </div>

          <div style={{ marginBottom: 12 }}>
            <div style={{ color: '#888', fontSize: 12, marginBottom: 4 }}>庫存利用率</div>
            <div style={{ 
              height: 8, 
              background: '#333', 
              borderRadius: 4, 
              overflow: 'hidden' 
            }}>
              <div style={{ 
                width: `${selectedZone.utilization * 100}%`, 
                height: '100%', 
                background: getStatusColor(selectedZone.status),
                transition: 'width 0.3s'
              }} />
            </div>
            <div style={{ color: '#fff', fontSize: 12, marginTop: 4 }}>
              {selectedZone.currentStock} / {selectedZone.maxCapacity} ({Math.round(selectedZone.utilization * 100)}%)
            </div>
          </div>

          <div style={{ marginBottom: 12 }}>
            <div style={{ color: '#888', fontSize: 12, marginBottom: 4 }}>容量</div>
            <div style={{ color: '#fff' }}>
              {selectedZone.size.width}m × {selectedZone.size.depth}m × {selectedZone.size.height}m
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default Warehouse3DViewer;