import { Injectable } from '@angular/core';
import { AdminDashboardDto } from '../../../core/models/models';
import jsPDF from 'jspdf';

// Brand tokens
const C = {
  primary:     '#2d3a8c', primaryDark: '#1a2460', primaryLight: '#4a5db5',
  accent:      '#c97d1b', success: '#2e7d32', error: '#c62828',
  bg:          '#f8f7f4', surface: '#ffffff', surfaceAlt: '#f2f0eb',
  border:      '#e0ddd6', textPri: '#1a1a2e', textSec: '#5a6278',
  textMuted:   '#9aa0b4', star: '#f59e0b',
};

// jsPDF does not support Unicode (Rs., stars) — use ASCII-safe equivalents
const fmt = {
  rupee: (n: number) => 'Rs. ' + n.toLocaleString('en-IN'),
  rating: (n: number) => n.toFixed(1) + ' / 5.0',
};

@Injectable({ providedIn: 'root' })
export class ReportService {

  downloadReport(d: AdminDashboardDto): void {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const W = 210, H = 297, M = 14, CW = W - M * 2;
    const now = new Date();

    // helpers
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
      draw(C.border);
      doc.line(x + 6 + doc.getTextWidth(text) + 3, y + 2.5, x + CW, y + 2.5);
    };
    const footer = (page: number, total: number) => {
      fill(C.primary); doc.rect(0, H - 10, W, 10, 'F');
      fill(C.accent);  doc.rect(0, H - 11, W, 1, 'F');
      doc.setFont('helvetica', 'normal'); doc.setFontSize(7); color(C.surface);
      doc.text(d.hotelName + '  -  Confidential Hotel Report', M, H - 4);
      color('#a0aad0');
      doc.text('Page ' + page + ' of ' + total, W - M, H - 4, { align: 'right' });
    };

    let y = 0;

    // ══════════════════════════════════════════════════════════════════════════
    // PAGE 1
    // ══════════════════════════════════════════════════════════════════════════

    // Cover band
    fill(C.primary); doc.rect(0, 0, W, 52, 'F');
    fill(C.primaryDark); doc.triangle(W - 55, 0, W, 0, W, 52, 'F');
    fill(C.accent); doc.rect(0, 52, W, 2.5, 'F');

    // Hotel initial circle
    fill(C.primaryLight); doc.circle(M + 10, 26, 10, 'F');
    doc.setFont('helvetica', 'bold'); doc.setFontSize(12); color(C.surface);
    doc.text(d.hotelName.charAt(0).toUpperCase(), M + 10, 29.5, { align: 'center' });

    // Hotel name & subtitle
    doc.setFont('helvetica', 'bold'); doc.setFontSize(19); color(C.surface);
    doc.text(d.hotelName, M + 24, 22);
    doc.setFont('helvetica', 'normal'); doc.setFontSize(9); color('#c8d0f0');
    doc.text('Hotel Performance & Analysis Report', M + 24, 30);
    doc.setFontSize(8); color('#a0aad0');
    doc.text('Generated: ' + now.toLocaleDateString('en-IN', { day:'2-digit', month:'long', year:'numeric' }), M + 24, 38);

    // Status pill
    const statusText  = d.isBlockedBySuperAdmin ? 'Blocked' : d.isActive ? 'Live' : 'Inactive';
    const statusColor = d.isBlockedBySuperAdmin ? C.error : d.isActive ? C.success : C.textMuted;
    fill(C.surface); doc.roundedRect(W - M - 24, 19, 24, 8, 2, 2, 'F');
    doc.setFont('helvetica', 'bold'); doc.setFontSize(7.5); color(statusColor);
    doc.text(statusText, W - M - 12, 24, { align: 'center' });

    y = 62;

    // ── KPI Cards (2x2) ───────────────────────────────────────────────────────
    secTitle('KEY PERFORMANCE INDICATORS', M, y); y += 7;

    const kpis = [
      { label: 'Active Rooms',      value: d.activeRooms + ' / ' + d.totalRooms,  sub: d.totalRoomTypes + ' room types',    accent: C.primary  },
      { label: 'Total Reservations',value: String(d.totalReservations),            sub: d.pendingReservations + ' pending',  accent: C.success  },
      { label: 'Total Revenue',     value: fmt.rupee(d.totalRevenue),              sub: 'Lifetime earnings',                 accent: C.accent   },
      { label: 'Average Rating',    value: fmt.rating(d.averageRating),            sub: d.totalReviews + ' guest reviews',   accent: C.star     },
    ];
    const kW = (CW - 6) / 2, kH = 22;
    kpis.forEach((k, i) => {
      const kx = M + (i % 2) * (kW + 6);
      const ky = y + Math.floor(i / 2) * (kH + 4);
      fill(C.surface); draw(C.border); doc.roundedRect(kx, ky, kW, kH, 2.5, 2.5, 'FD');
      fill(k.accent); doc.roundedRect(kx, ky, 3, kH, 1.5, 1.5, 'F');
      doc.setFont('helvetica', 'bold'); doc.setFontSize(11); color(k.accent);
      doc.text(k.value, kx + 7, ky + 9);
      doc.setFont('helvetica', 'normal'); doc.setFontSize(7); color(C.textSec);
      doc.text(k.label.toUpperCase(), kx + 7, ky + 14.5);
      doc.setFontSize(6.5); color(C.textMuted);
      doc.text(k.sub, kx + 7, ky + 19);
    });
    y += 2 * (kH + 4) + 8;

    // ── Reservation Status Bars ────────────────────────────────────────────────
    secTitle('RESERVATION BREAKDOWN', M, y); y += 7;

    const statuses = [
      { label: 'Pending',   value: d.pendingReservations,   color: C.star    },
      { label: 'Confirmed', value: d.activeReservations,    color: C.success },
      { label: 'Completed', value: d.completedReservations, color: C.primary },
      { label: 'Cancelled', value: d.cancelledReservations, color: C.error   },
    ];
    const total  = d.totalReservations || 1;
    const maxVal = Math.max(...statuses.map(s => s.value), 1);
    const barAreaW = CW - 38;

    statuses.forEach((s, i) => {
      const by = y + i * 11;
      const bw = Math.max((s.value / maxVal) * barAreaW, s.value > 0 ? 2 : 0);
      doc.setFont('helvetica', 'normal'); doc.setFontSize(8); color(C.textSec);
      doc.text(s.label, M, by + 5.5);
      fill(C.surfaceAlt); draw(C.border); doc.roundedRect(M + 22, by + 1, barAreaW, 7, 1.5, 1.5, 'FD');
      if (s.value > 0) { fill(s.color); doc.roundedRect(M + 22, by + 1, bw, 7, 1.5, 1.5, 'F'); }
      doc.setFont('helvetica', 'bold'); doc.setFontSize(8); color(C.textPri);
      doc.text(String(s.value), M + 22 + barAreaW + 3, by + 6);
      doc.setFont('helvetica', 'normal'); doc.setFontSize(7); color(C.textMuted);
      doc.text('(' + ((s.value / total) * 100).toFixed(0) + '%)', M + 22 + barAreaW + 11, by + 6);
    });
    y += statuses.length * 11 + 8;

    // ── Donut + Legend ────────────────────────────────────────────────────────
    secTitle('RESERVATION DISTRIBUTION', M, y); y += 7;

    const { c: dc, ctx: dctx } = mkCanvas(220, 220);
    dctx.fillStyle = C.surface; dctx.fillRect(0, 0, 220, 220);
    const cx = 110, cy = 110, outerR = 95, innerR = 58;
    let sa = -Math.PI / 2;
    statuses.filter(s => s.value > 0).forEach(s => {
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
    dctx.fillText(String(d.totalReservations), cx, cy + 6);
    dctx.font = '13px Arial'; dctx.fillStyle = C.textMuted; dctx.fillText('Total', cx, cy + 22);
    doc.addImage(dc.toDataURL('image/png'), 'PNG', M, y, 44, 44);

    let ly = y + 2;
    statuses.filter(s => s.value > 0).forEach(s => {
      fill(s.color); doc.roundedRect(M + 48, ly, 4, 4, 1, 1, 'F');
      doc.setFont('helvetica', 'bold'); doc.setFontSize(8); color(C.textPri);
      doc.text(s.label, M + 55, ly + 3.5);
      doc.setFont('helvetica', 'normal'); color(C.textMuted);
      doc.text(s.value + '  (' + ((s.value / total) * 100).toFixed(1) + '%)', M + 80, ly + 3.5);
      ly += 9;
    });
    y += 50;

    // ── Financial Summary Table ────────────────────────────────────────────────
    secTitle('FINANCIAL SUMMARY', M, y); y += 7;

    const finRows: [string, string, string][] = [
      ['Total Revenue',  fmt.rupee(d.totalRevenue),          C.accent  ],
      ['Active Rooms',   d.activeRooms + ' of ' + d.totalRooms, C.primary],
      ['Room Types',     String(d.totalRoomTypes),            C.primary ],
      ['Total Reviews',  String(d.totalReviews),              C.star    ],
      ['Average Rating', fmt.rating(d.averageRating),         C.star    ],
    ];
    finRows.forEach(([label, val, accent], i) => {
      const ry = y + i * 9;
      fill(i % 2 === 0 ? C.surfaceAlt : C.surface); draw(C.border); doc.rect(M, ry, CW, 8.5, 'FD');
      doc.setFont('helvetica', 'normal'); doc.setFontSize(8.5); color(C.textSec);
      doc.text(label, M + 4, ry + 5.5);
      doc.setFont('helvetica', 'bold'); doc.setFontSize(9); color(accent);
      doc.text(val, M + CW - 4, ry + 5.5, { align: 'right' });
    });
    y += finRows.length * 9 + 8;

    // ── Rating Bar ────────────────────────────────────────────────────────────
    secTitle('GUEST SATISFACTION', M, y); y += 7;

    // Star row using text dots instead of unicode stars
    const starFull = Math.floor(d.averageRating);
    doc.setFont('helvetica', 'bold'); doc.setFontSize(14);
    for (let i = 0; i < 5; i++) {
      color(i < starFull ? C.star : C.border);
      doc.text('*', M + i * 8, y + 8);
    }
    doc.setFontSize(14); color(C.textPri);
    doc.text(fmt.rating(d.averageRating), M + 44, y + 8);
    y += 13;

    fill(C.surfaceAlt); draw(C.border); doc.roundedRect(M, y, CW, 8, 4, 4, 'FD');
    fill(C.star); doc.roundedRect(M, y, CW * (d.averageRating / 5), 8, 4, 4, 'F');
    doc.setFont('helvetica', 'bold'); doc.setFontSize(7.5); color(C.surface);
    doc.text(((d.averageRating / 5) * 100).toFixed(0) + '% satisfaction', M + 4, y + 5.5);
    y += 14;

    footer(1, 2);

    // ══════════════════════════════════════════════════════════════════════════
    // PAGE 2 — Charts
    // ══════════════════════════════════════════════════════════════════════════
    doc.addPage(); y = 0;

    fill(C.primary); doc.rect(0, 0, W, 18, 'F');
    fill(C.accent);  doc.rect(0, 18, W, 1.5, 'F');
    doc.setFont('helvetica', 'bold'); doc.setFontSize(10); color(C.surface);
    doc.text(d.hotelName + '  -  Charts & Visual Analysis', M, 12);
    y = 28;

    // ── Bar chart ─────────────────────────────────────────────────────────────
    secTitle('RESERVATION STATUS - BAR CHART', M, y); y += 7;

    const { c: bc, ctx: bctx } = mkCanvas(740, 260);
    bctx.fillStyle = C.bg; bctx.fillRect(0, 0, 740, 260);
    const bColors = [C.star, C.success, C.primary, C.error];
    const bVals   = [d.pendingReservations, d.activeReservations, d.completedReservations, d.cancelledReservations];
    const bLabels = ['Pending', 'Confirmed', 'Completed', 'Cancelled'];
    const bMax    = Math.max(...bVals, 1);
    const bW2 = 80, bGap = 30, bStartX = 60, bBaseY = 220;

    bVals.forEach((v, i) => {
      const bx = bStartX + i * (bW2 + bGap);
      const bh = (v / bMax) * 180;
      const by2 = bBaseY - bh;
      bctx.fillStyle = 'rgba(0,0,0,0.05)'; bctx.fillRect(bx + 3, by2 + 3, bW2, bh);
      bctx.fillStyle = bColors[i];
      bctx.beginPath();
      bctx.rect(bx, by2, bW2, bh);
      bctx.fill();
      bctx.fillStyle = C.textPri; bctx.font = 'bold 16px Arial'; bctx.textAlign = 'center';
      bctx.fillText(String(v), bx + bW2 / 2, by2 - 8);
      bctx.fillStyle = C.textMuted; bctx.font = '12px Arial';
      bctx.fillText(((v / total) * 100).toFixed(0) + '%', bx + bW2 / 2, by2 - 22);
      bctx.fillStyle = C.textSec; bctx.font = '13px Arial';
      bctx.fillText(bLabels[i], bx + bW2 / 2, bBaseY + 18);
    });
    bctx.strokeStyle = C.border; bctx.lineWidth = 1.5;
    bctx.beginPath(); bctx.moveTo(40, bBaseY); bctx.lineTo(700, bBaseY); bctx.stroke();

    doc.addImage(bc.toDataURL('image/png'), 'PNG', M, y, CW, CW * 260 / 740);
    y += CW * 260 / 740 + 10;

    // ── Rooms overview ────────────────────────────────────────────────────────
    secTitle('ROOMS OVERVIEW', M, y); y += 7;

    const roomRows = [
      { label: 'Active Rooms',   value: d.activeRooms,                max: d.totalRooms, color: C.success },
      { label: 'Inactive Rooms', value: d.totalRooms - d.activeRooms, max: d.totalRooms, color: C.error   },
    ];
    const rBarW = CW - 50;
    roomRows.forEach((r, i) => {
      const ry2 = y + i * 12;
      const rw  = r.max > 0 ? (r.value / r.max) * rBarW : 0;
      doc.setFont('helvetica', 'normal'); doc.setFontSize(8); color(C.textSec);
      doc.text(r.label, M, ry2 + 6);
      fill(C.surfaceAlt); draw(C.border); doc.roundedRect(M + 32, ry2, rBarW, 8, 2, 2, 'FD');
      if (r.value > 0) { fill(r.color); doc.roundedRect(M + 32, ry2, rw, 8, 2, 2, 'F'); }
      doc.setFont('helvetica', 'bold'); doc.setFontSize(8); color(C.textPri);
      doc.text(r.value + ' / ' + r.max, M + 32 + rBarW + 3, ry2 + 6);
    });
    y += roomRows.length * 12 + 10;

    // ── Rating gauge ──────────────────────────────────────────────────────────
    secTitle('RATING GAUGE', M, y); y += 7;

    const { c: rc, ctx: rctx } = mkCanvas(600, 110);
    rctx.fillStyle = C.bg; rctx.fillRect(0, 0, 600, 110);
    const gcx = 300, gcy = 95, gr = 72;
    rctx.beginPath(); rctx.arc(gcx, gcy, gr, Math.PI, 2 * Math.PI);
    rctx.strokeStyle = C.border; rctx.lineWidth = 14; rctx.stroke();
    const gAngle = Math.PI + (d.averageRating / 5) * Math.PI;
    rctx.beginPath(); rctx.arc(gcx, gcy, gr, Math.PI, gAngle);
    rctx.strokeStyle = C.star; rctx.lineWidth = 14; rctx.lineCap = 'round'; rctx.stroke();
    rctx.fillStyle = C.textPri; rctx.font = 'bold 24px Arial'; rctx.textAlign = 'center';
    rctx.fillText(d.averageRating.toFixed(1), gcx, gcy - 8);
    rctx.font = '13px Arial'; rctx.fillStyle = C.textMuted; rctx.fillText('out of 5.0', gcx, gcy + 10);
    rctx.fillStyle = C.textSec; rctx.font = '12px Arial';
    rctx.fillText('1.0', gcx - gr - 12, gcy + 4);
    rctx.fillText('5.0', gcx + gr + 12, gcy + 4);

    doc.addImage(rc.toDataURL('image/png'), 'PNG', M + CW / 2 - 42, y, 84, 84 * 110 / 600);
    y += 84 * 110 / 600 + 8;

    // ── Disclaimer ────────────────────────────────────────────────────────────
    fill(C.surfaceAlt); draw(C.border); doc.roundedRect(M, y, CW, 14, 2, 2, 'FD');
    doc.setFont('helvetica', 'italic'); doc.setFontSize(7.5); color(C.textMuted);
    doc.text('This report is auto-generated from live hotel data. All figures are cumulative lifetime totals.', M + 4, y + 5.5);
    doc.text('Confidential - for internal use only. (c) ' + now.getFullYear() + ' HotelBooking Platform.', M + 4, y + 10.5);

    footer(2, 2);

    doc.save(d.hotelName.replace(/\s+/g, '_') + '_Report_' + now.toISOString().slice(0,10) + '.pdf');
  }
}
