/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: ["class"],
  content: [
    './Components/**/*.{razor,html,cs}',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: "var(--font-sans, ui-sans-serif, system-ui, sans-serif)",
        serif: "var(--font-serif, ui-serif, Georgia, serif)",
        mono: "var(--font-mono, ui-monospace, monospace)",
      },
      colors: {
        border: "var(--border)",
        input: "var(--input)",
        ring: "var(--ring)",
        background: "var(--background)",
        foreground: "var(--foreground)",
        primary: {
          DEFAULT: "var(--primary)",
          foreground: "var(--primary-foreground)",
        },
        secondary: {
          DEFAULT: "var(--secondary)",
          foreground: "var(--secondary-foreground)",
        },
        destructive: {
          DEFAULT: "var(--destructive)",
          foreground: "var(--destructive-foreground)",
        },
        muted: {
          DEFAULT: "var(--muted)",
          foreground: "var(--muted-foreground)",
        },
        accent: {
          DEFAULT: "var(--accent)",
          foreground: "var(--accent-foreground)",
        },
        popover: {
          DEFAULT: "var(--popover)",
          foreground: "var(--popover-foreground)",
        },
        card: {
          DEFAULT: "var(--card)",
          foreground: "var(--card-foreground)",
        },
        sidebar: {
          DEFAULT: "var(--sidebar)",
          foreground: "var(--sidebar-foreground)",
          primary: "var(--sidebar-primary)",
          "primary-foreground": "var(--sidebar-primary-foreground)",
          accent: "var(--sidebar-accent)",
          "accent-foreground": "var(--sidebar-accent-foreground)",
          border: "var(--sidebar-border)",
          ring: "var(--sidebar-ring)",
        },
        alert: {
          success: {
            DEFAULT: "var(--alert-success)",
            foreground: "var(--alert-success-foreground)",
            bg: "var(--alert-success-bg)",
          },
          info: {
            DEFAULT: "var(--alert-info)",
            foreground: "var(--alert-info-foreground)",
            bg: "var(--alert-info-bg)",
          },
          warning: {
            DEFAULT: "var(--alert-warning)",
            foreground: "var(--alert-warning-foreground)",
            bg: "var(--alert-warning-bg)",
          },
          danger: {
            DEFAULT: "var(--alert-danger)",
            foreground: "var(--alert-danger-foreground)",
            bg: "var(--alert-danger-bg)",
          },
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      width: {
        "sidebar": "var(--sidebar-width)",
        "sidebar-mobile": "var(--sidebar-width-mobile)",
        "sidebar-icon": "var(--sidebar-width-icon)",
      },
      animationDuration: {
        200: '200ms',
        300: '300ms',
      },
      animationTimingFunction: {
        DEFAULT: 'ease-out',
      },
      keyframes: {
        "accordion-down": {
          from: { height: "0" },
          to: { height: "var(--radix-accordion-content-height, auto)" },
        },
        "accordion-up": {
          from: { height: "var(--radix-accordion-content-height, auto)" },
          to: { height: "0" },
        },
        "collapsible-down": {
          from: { height: "0" },
          to: { height: "var(--radix-collapsible-content-height, auto)" },
        },
        "collapsible-up": {
          from: { height: "var(--radix-collapsible-content-height, auto)" },
          to: { height: "0" },
        },
        // Dialog animations
        "enter": {
          from: { opacity: "0", transform: "scale(0.95)" },
          to: { opacity: "1", transform: "scale(1)" },
        },
        "exit": {
          from: { opacity: "1", transform: "scale(1)" },
          to: { opacity: "0", transform: "scale(0.95)" },
        },
        "fade-in": {
          from: { opacity: "0" },
          to: { opacity: "1" },
        },
        "fade-out": {
          from: { opacity: "1" },
          to: { opacity: "0" },
        },
        "zoom-in": {
          from: { opacity: "0", transform: "scale(0.95)" },
          to: { opacity: "1", transform: "scale(1)" },
        },
        "zoom-out": {
          from: { opacity: "1", transform: "scale(1)" },
          to: { opacity: "0", transform: "scale(0.95)" },
        },
        "slide-in-from-top": {
          from: { transform: "translateY(-100%)" },
          to: { transform: "translateY(0)" },
        },
        "slide-in-from-bottom": {
          from: { transform: "translateY(100%)" },
          to: { transform: "translateY(0)" },
        },
        "slide-in-from-left": {
          from: { transform: "translateX(-100%)" },
          to: { transform: "translateX(0)" },
        },
        "slide-in-from-right": {
          from: { transform: "translateX(100%)" },
          to: { transform: "translateX(0)" },
        },
        "slide-out-to-top": {
          from: { transform: "translateY(0)" },
          to: { transform: "translateY(-100%)" },
        },
        "slide-out-to-bottom": {
          from: { transform: "translateY(0)" },
          to: { transform: "translateY(100%)" },
        },
        "slide-out-to-left": {
          from: { transform: "translateX(0)" },
          to: { transform: "translateX(-100%)" },
        },
        "slide-out-to-right": {
          from: { transform: "translateX(0)" },
          to: { transform: "translateX(100%)" },
        },
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out",
        "collapsible-down": "collapsible-down 0.2s ease-out",
        "collapsible-up": "collapsible-up 0.2s ease-out",
        // Dialog animations
        "in": "enter 0.2s ease-out",
        "out": "exit 0.2s ease-out",
        "fade-in": "fade-in 0.2s ease-out",
        "fade-out": "fade-out 0.2s ease-out",
        "zoom-in": "zoom-in 0.2s ease-out",
        "zoom-out": "zoom-out 0.2s ease-out",
        "slide-in-from-top": "slide-in-from-top 0.2s ease-out",
        "slide-in-from-bottom": "slide-in-from-bottom 0.2s ease-out",
        "slide-in-from-left": "slide-in-from-left 0.2s ease-out",
        "slide-in-from-right": "slide-in-from-right 0.2s ease-out",
        "slide-out-to-top": "slide-out-to-top 0.2s ease-out",
        "slide-out-to-bottom": "slide-out-to-bottom 0.2s ease-out",
        "slide-out-to-left": "slide-out-to-left 0.2s ease-out",
        "slide-out-to-right": "slide-out-to-right 0.2s ease-out",
      },
    },
  },
  plugins: [
    function({ addUtilities, theme }) {
      // Generate fade-in/out variants
      const fadeVariants = {
        '.fade-in-0': {
          animationName: 'fade-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.fade-in-50': {
          animationName: 'fade-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.fade-in-100': {
          animationName: 'fade-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.fade-out-0': {
          animationName: 'fade-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.fade-out-50': {
          animationName: 'fade-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.fade-out-100': {
          animationName: 'fade-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
      };

      // Generate zoom-in/out variants
      const zoomVariants = {
        '.zoom-in-0': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-in-50': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-in-75': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-in-90': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-in-95': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-in-100': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-in-105': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-in-110': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-in-125': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-in-150': {
          animationName: 'zoom-in',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-0': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-50': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-75': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-90': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-95': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-100': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-105': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-110': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-125': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.zoom-out-150': {
          animationName: 'zoom-out',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
      };

      // Generate slide variants
      const slideVariants = {
        '.slide-in-from-top-0': {
          animationName: 'slide-in-from-top',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-top-1\\/2': {
          animationName: 'slide-in-from-top',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-top-full': {
          animationName: 'slide-in-from-top',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-top-\\[48\\%\\]': {
          animationName: 'slide-in-from-top',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-bottom-0': {
          animationName: 'slide-in-from-bottom',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-bottom-1\\/2': {
          animationName: 'slide-in-from-bottom',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-bottom-full': {
          animationName: 'slide-in-from-bottom',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-bottom-\\[48\\%\\]': {
          animationName: 'slide-in-from-bottom',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-left-0': {
          animationName: 'slide-in-from-left',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-left-1\\/2': {
          animationName: 'slide-in-from-left',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-left-full': {
          animationName: 'slide-in-from-left',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-left-\\[48\\%\\]': {
          animationName: 'slide-in-from-left',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-right-0': {
          animationName: 'slide-in-from-right',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-right-1\\/2': {
          animationName: 'slide-in-from-right',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-right-full': {
          animationName: 'slide-in-from-right',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-in-from-right-\\[48\\%\\]': {
          animationName: 'slide-in-from-right',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-top-0': {
          animationName: 'slide-out-to-top',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-top-1\\/2': {
          animationName: 'slide-out-to-top',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-top-full': {
          animationName: 'slide-out-to-top',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-top-\\[48\\%\\]': {
          animationName: 'slide-out-to-top',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-bottom-0': {
          animationName: 'slide-out-to-bottom',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-bottom-1\\/2': {
          animationName: 'slide-out-to-bottom',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-bottom-full': {
          animationName: 'slide-out-to-bottom',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-bottom-\\[48\\%\\]': {
          animationName: 'slide-out-to-bottom',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-left-0': {
          animationName: 'slide-out-to-left',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-left-1\\/2': {
          animationName: 'slide-out-to-left',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-left-full': {
          animationName: 'slide-out-to-left',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-left-\\[48\\%\\]': {
          animationName: 'slide-out-to-left',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-right-0': {
          animationName: 'slide-out-to-right',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-right-1\\/2': {
          animationName: 'slide-out-to-right',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-right-full': {
          animationName: 'slide-out-to-right',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
        '.slide-out-to-right-\\[48\\%\\]': {
          animationName: 'slide-out-to-right',
          animationDuration: '0.2s',
          animationTimingFunction: 'ease-out',
        },
      };

      addUtilities({
        ...fadeVariants,
        ...zoomVariants,
        ...slideVariants,
      });
    },
  ],
}
