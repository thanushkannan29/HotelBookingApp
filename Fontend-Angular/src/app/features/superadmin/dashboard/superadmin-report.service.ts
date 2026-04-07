import { Injectable } from '@angular/core';
import { SuperAdminDashboardDto } from '../../../core/models/models';
import jsPDF from 'jspdf';

const C = {
  primary: '#2d3a8c', primaryDark: '#1a2460', primaryLight: '#4a5db5',
  accent: '#c97d1b', success: '#2e7d32', error: '#c62828',
  bg: '#f8f7f4', surface: '#ffffff', surfaceAlt: '#f2f0eb',
  border: '#e0ddd6', textPri: '#1a1a2e', textSec: '#5a6278',
  textMuted: '#9aa0b4', star: '#f59e0b',
};

@Injectable({ providedIn: 'root' })
export class SuperadminReportService {

  downloadReport(d: SuperAdminDashboardDto, commissionEarned: number): void {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const W = 210, H = 297, M = 14, CW = W - M * 2;
    const now = new Date();

    const rgb = (h: string): [number, number, number] => [
      parseInt(h.slice(1,3),16), parseInt(h.slice(3,5),16), parseInt(h.slice(5,7),16)
    ];
    const fill  = (h: string) => doc.setFillColor(...rgb(h));
    const draw  = (h: string) => doc.setDrawColor(...rgb(h));
    const color = (h: string) => doc.setTextColor(...rgb(h));
    const mkCanvas = (w: number, h: number) => {
      const c = document.createElement('canvas'); c.width = w; c.height = h;
      return { c, ctx: c.getContext('2d')! };
    };
    const secTitle = (text: string, x: number, y: number) => {
      fill(C.primary); doc.rect(x, y, 3, 5, 'F');
      doc.setFont('helvetica', 'bold'); doc.setFontSize(8.5); color(C.primary);
      doc.text(text, x + 6, y + 4);
      draw(C.border); doc.line(x + 6 + doc.getTextWidth(text) + 3, y + 2.5, x + CW, y + 2.5);
    };
    const footer = (page: number, total: number) => {
      fill(C.primary); doc.rect(0, H - 10, W, 10, 'F');
      fill(C.accent);  doc.rect(0, H - 11, W, 1, 'F');
      doc.setFont('helvetica', 'normal'); doc.setFontSize(7); color(C.surface);
      doc.text('Thanush StayHub Platform  -  SuperAdmin Report', M, H - 4);
      color('#a0aad0');
      doc.text('Page ' + page + ' of ' + total, W - M, H - 4, { align: 'right' });
    };

    let y = 0;

    // ── Cover ─────────────────────────────────────────────────────────────────
    fill(C.primary); doc.rect(0, 0, W, 52, 'F');
    fill(C.primaryDark); doc.triangle(W - 55, 0, W, 0, W, 52, 'F');
    fill(C.accent); doc.rect(0, 52, W, 2.5, 'F');

    fill(C.primaryLight); doc.circle(M + 10, 26, 10, 'F');
    doc.setFont('helvetica', 'bold'); doc.setFontSize(14); color(C.surface);
    doc.text('SA', M + 10, 29.5, { align: 'center' });

    doc.setFont('helvetica', 'bold'); doc.setFontSize(19); color(C.surface);
    doc.text('Thanush StayHub Platform', M + 24, 22);
    doc.setFont('helvetica', 'normal'); doc.setFontSize(9); color('#c8d0f0');
    doc.text('SuperAdmin Platform-Wide Analysis Report', M + 24, 30);
    doc.setFontSize(8); color('#a0aad0');
    doc.text('Generated: ' + now.toLocaleDateString('en-IN', { day:'2-digit', month:'long', year:'numeric' }), M + 24, 38);

    y = 62;

    // ── KPI Cards ─────────────────────────────────────────────────────────────
    secTitle('PLATFORM KEY METRICS', M, y); y += 7;

    const kpis = [
      { label: 'Total Hotels',    value: String(d.totalHotels),                          sub: d.activeHotels + ' active, ' + d.blockedHotels + ' blocked', accent: C.primary  },
      { label: 'Total Users',     value: String(d.totalUsers),                           sub: 'Registered guests & admins',                                accent: C.success  },
      { label: 'Total Revenue',   value: 'Rs. ' + d.totalRevenue.toLocaleString('en-IN'), sub: 'Platform-wide bookings',                                   accent: C.accent   },
      { label: 'Commission Earned', value: 'Rs. ' + commissionEarned.toLocaleString('en-IN'), sub: '2% of completed reservations',                         accent: '#1565c0'  },
      { label: 'Reservations',    value: String(d.totalReservations),                    sub: 'All time',                                                  accent: '#1565c0'  },
      { label: 'Total Reviews',   value: String(d.totalReviews),                         sub: 'Guest feedback',                                            accent: C.star     },
    ];
    const kW = (CW - 6) / 2, kH = 20;
    kpis.forEach((k, i) => {
      const kx = M + (i % 2) * (kW + 6);
      const ky = y + Math.floor(i / 2) * (kH + 4);
      fill(C.surface); draw(C.border); doc.roundedRect(kx, ky, kW, kH, 2.5, 2.5, 'FD');
      fill(k.accent); doc.roundedRect(kx, ky, 3, kH, 1.5, 1.5, 'F');
      doc.setFont('helvetica', 'bold'); doc.setFontSize(11); color(k.accent);
      doc.text(k.value, kx + 7, ky + 8);
      doc.setFont('helvetica', 'normal'); doc.setFontSize(7); color(C.textSec);
      doc.text(k.label.toUpperCase(), kx + 7, ky + 13);
      doc.setFontSize(6.5); color(C.textMuted);
      doc.text(k.sub, kx + 7, ky + 17);
    });
    y += 3 * (kH + 4) + 8;

    // ── Hotel Status Breakdown ─────────────────────────────────────────────────
    secTitle('HOTEL STATUS BREAKDOWN', M, y); y += 7;

    const hotelStatuses = [
      { label: 'Active',   value: d.activeHotels,                          color: C.success },
      { label: 'Inactive', value: d.totalHotels - d.activeHotels - d.blockedHotels, color: C.star },
      { label: 'Blocked',  value: d.blockedHotels,                         color: C.error   },
    ];
    const maxH = Math.max(...hotelStatuses.map(s => s.value), 1);
    const barAreaW = CW - 38;

    hotelStatuses.forEach((s, i) => {
      const by = y + i * 11;
      const bw = Math.max((s.value / maxH) * barAreaW, s.value > 0 ? 2 : 0);
      doc.setFont('helvetica', 'normal'); doc.setFontSize(8); color(C.textSec);
      doc.text(s.label, M, by + 5.5);
      fill(C.surfaceAlt); draw(C.border); doc.roundedRect(M + 22, by + 1, barAreaW, 7, 1.5, 1.5, 'FD');
      if (s.value > 0) { fill(s.color); doc.roundedRect(M + 22, by + 1, bw, 7, 1.5, 1.5, 'F'); }
      doc.setFont('helvetica', 'bold'); doc.setFontSize(8); color(C.textPri);
      doc.text(String(s.value), M + 22 + barAreaW + 3, by + 6);
      doc.setFont('helvetica', 'normal'); doc.setFontSize(7); color(C.textMuted);
      const pct = d.totalHotels > 0 ? ((s.value / d.totalHotels) * 100).toFixed(0) : '0';
      doc.text('(' + pct + '%)', M + 22 + barAreaW + 11, by + 6);
    });
    y += hotelStatuses.length * 11 + 8;

    // ── Donut: Hotel distribution ──────────────────────────────────────────────
    secTitle('HOTEL DISTRIBUTION', M, y); y += 7;

    const { c: dc, ctx: dctx } = mkCanvas(220, 220);
    dctx.fillStyle = C.surface; dctx.fillRect(0, 0, 220, 220);
    const cx = 110, cy = 110, outerR = 95, innerR = 58;
    let sa = -Math.PI / 2;
    const total = d.totalHotels || 1;
    hotelStatuses.filter(s => s.value > 0).forEach(s => {
      const angle = (s.value / total) * 2 * Math.PI;
      dctx.beginPath(); dctx.moveTo(cx, cy);
      dctx.arc(cx, cy, outerR, sa, sa + angle); dctx.closePath();
      dctx.fillStyle = s.color; dctx.fill(); sa += angle;
    });
    dctx.beginPath(); dctx.arc(cx, cy, outerR - 2, 0, 2 * Math.PI);
    dctx.strokeStyle = C.surface; dctx.lineWidth = 3; dctx.stroke();
    dctx.beginPath(); dctx.arc(cx, cy, innerR, 0, 2 * Math.PI);
    dctx.fillStyle = C.surface; dctx.fill();
    dctx.fillStyle = C.textPri; dctx.font = 'bold 26px Arial'; dctx.textAlign = 'center';
    dctx.fillText(String(d.totalHotels), cx, cy + 6);
    dctx.font = '13px Arial'; dctx.fillStyle = C.textMuted; dctx.fillText('Hotels', cx, cy + 22);
    doc.addImage(dc.toDataURL('image/png'), 'PNG', M, y, 44, 44);

    let ly = y + 2;
    hotelStatuses.filter(s => s.value > 0).forEach(s => {
      fill(s.color); doc.roundedRect(M + 48, ly, 4, 4, 1, 1, 'F');
      doc.setFont('helvetica', 'bold'); doc.setFontSize(8); color(C.textPri);
      doc.text(s.label, M + 55, ly + 3.5);
      doc.setFont('helvetica', 'normal'); color(C.textMuted);
      doc.text(s.value + '  (' + ((s.value / total) * 100).toFixed(1) + '%)', M + 80, ly + 3.5);
      ly += 9;
    });
    y += 50;

    // ── Summary Table ─────────────────────────────────────────────────────────
    secTitle('PLATFORM SUMMARY', M, y); y += 7;

    const rows: [string, string, string][] = [
      ['Total Hotels',       String(d.totalHotels),                              C.primary],
      ['Active Hotels',      String(d.activeHotels),                             C.success],
      ['Blocked Hotels',     String(d.blockedHotels),                            C.error  ],
      ['Total Users',        String(d.totalUsers),                               C.primary],
      ['Total Reservations', String(d.totalReservations),                        '#1565c0' ],
      ['Platform Revenue',   'Rs. ' + d.totalRevenue.toLocaleString('en-IN'),    C.accent ],
      ['Commission Earned',  'Rs. ' + commissionEarned.toLocaleString('en-IN'),  '#1565c0' ],
      ['Total Reviews',      String(d.totalReviews),                             C.star   ],
    ];
    rows.forEach(([label, val, accent], i) => {
      const ry = y + i * 9;
      fill(i % 2 === 0 ? C.surfaceAlt : C.surface); draw(C.border); doc.rect(M, ry, CW, 8.5, 'FD');
      doc.setFont('helvetica', 'normal'); doc.setFontSize(8.5); color(C.textSec);
      doc.text(label, M + 4, ry + 5.5);
      doc.setFont('helvetica', 'bold'); doc.setFontSize(9); color(accent);
      doc.text(val, M + CW - 4, ry + 5.5, { align: 'right' });
    });
    y += rows.length * 9 + 8;

    footer(1, 2);

    // ── PAGE 2: Charts ─────────────────────────────────────────────────────────
    doc.addPage(); y = 0;

    fill(C.primary); doc.rect(0, 0, W, 18, 'F');
    fill(C.accent);  doc.rect(0, 18, W, 1.5, 'F');
    doc.setFont('helvetica', 'bold'); doc.setFontSize(10); color(C.surface);
    doc.text('Thanush StayHub Platform  -  Charts & Visual Analysis', M, 12);
    y = 28;

    // Bar chart: hotel status
    secTitle('HOTEL STATUS - BAR CHART', M, y); y += 7;

    const { c: bc, ctx: bctx } = mkCanvas(740, 220);
    bctx.fillStyle = C.bg; bctx.fillRect(0, 0, 740, 220);
    const bColors = [C.success, C.star, C.error];
    const bVals   = [d.activeHotels, d.totalHotels - d.activeHotels - d.blockedHotels, d.blockedHotels];
    const bLabels = ['Active', 'Inactive', 'Blocked'];
    const bMax    = Math.max(...bVals, 1);
    const bW2 = 100, bGap = 60, bStartX = 100, bBaseY = 180;

    bVals.forEach((v, i) => {
      const bx = bStartX + i * (bW2 + bGap);
      const bh = (v / bMax) * 150;
      const by2 = bBaseY - bh;
      bctx.fillStyle = 'rgba(0,0,0,0.05)'; bctx.fillRect(bx + 3, by2 + 3, bW2, bh);
      bctx.fillStyle = bColors[i]; bctx.fillRect(bx, by2, bW2, bh);
      bctx.fillStyle = C.textPri; bctx.font = 'bold 18px Arial'; bctx.textAlign = 'center';
      bctx.fillText(String(v), bx + bW2 / 2, by2 - 8);
      bctx.fillStyle = C.textSec; bctx.font = '14px Arial';
      bctx.fillText(bLabels[i], bx + bW2 / 2, bBaseY + 18);
    });
    bctx.strokeStyle = C.border; bctx.lineWidth = 1.5;
    bctx.beginPath(); bctx.moveTo(60, bBaseY); bctx.lineTo(680, bBaseY); bctx.stroke();

    doc.addImage(bc.toDataURL('image/png'), 'PNG', M, y, CW, CW * 220 / 740);
    y += CW * 220 / 740 + 10;

    // Revenue vs Commission gauge
    secTitle('REVENUE vs COMMISSION', M, y); y += 7;

    const { c: rc, ctx: rctx } = mkCanvas(600, 110);
    rctx.fillStyle = C.bg; rctx.fillRect(0, 0, 600, 110);
    const gcx = 300, gcy = 95, gr = 72;
    rctx.beginPath(); rctx.arc(gcx, gcy, gr, Math.PI, 2 * Math.PI);
    rctx.strokeStyle = C.border; rctx.lineWidth = 14; rctx.stroke();
    const commPct = d.totalRevenue > 0 ? commissionEarned / d.totalRevenue : 0;
    const gAngle = Math.PI + commPct * Math.PI;
    rctx.beginPath(); rctx.arc(gcx, gcy, gr, Math.PI, gAngle);
    rctx.strokeStyle = '#1565c0'; rctx.lineWidth = 14; rctx.lineCap = 'round'; rctx.stroke();
    rctx.fillStyle = C.textPri; rctx.font = 'bold 18px Arial'; rctx.textAlign = 'center';
    rctx.fillText((commPct * 100).toFixed(1) + '%', gcx, gcy - 8);
    rctx.font = '12px Arial'; rctx.fillStyle = C.textMuted; rctx.fillText('Commission Rate', gcx, gcy + 8);
    rctx.fillStyle = C.textSec; rctx.font = '11px Arial';
    rctx.fillText('0%', gcx - gr - 12, gcy + 4);
    rctx.fillText('100%', gcx + gr + 12, gcy + 4);

    doc.addImage(rc.toDataURL('image/png'), 'PNG', M + CW / 2 - 42, y, 84, 84 * 110 / 600);
    y += 84 * 110 / 600 + 8;

    // Disclaimer
    fill(C.surfaceAlt); draw(C.border); doc.roundedRect(M, y, CW, 14, 2, 2, 'FD');
    doc.setFont('helvetica', 'italic'); doc.setFontSize(7.5); color(C.textMuted);
    doc.text('This report is auto-generated from live platform data. All figures are cumulative lifetime totals.', M + 4, y + 5.5);
    doc.text('Confidential - SuperAdmin use only. (c) ' + now.getFullYear() + ' Thanush StayHub Platform.', M + 4, y + 10.5);

    footer(2, 2);

    doc.save('ThanushStayHub_SuperAdmin_Report_' + now.toISOString().slice(0,10) + '.pdf');
  }
}
