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
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out",
        "collapsible-down": "collapsible-down 0.2s ease-out",
        "collapsible-up": "collapsible-up 0.2s ease-out",
      },
    },
  },
  plugins: [
    function({ addUtilities, matchUtilities }) {
      // Add animate-in and animate-out utilities
      addUtilities({
        '@keyframes enter': {
          from: { opacity: '0', transform: 'scale(0.95)' }
        },
        '@keyframes exit': {
          to: { opacity: '0', transform: 'scale(0.95)' }
        },
        '.animate-in': {
          animationName: 'enter',
          animationDuration: '200ms',
          animationTimingFunction: 'ease-out',
        },
        '.animate-out': {
          animationName: 'exit',
          animationDuration: '200ms',
          animationTimingFunction: 'ease-out',
        },
      });

      // Add fade utilities
      matchUtilities(
        {
          'fade-in': (value) => ({
            animationName: 'fade-in',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes fade-in': {
              from: { opacity: value },
            },
          }),
          'fade-out': (value) => ({
            animationName: 'fade-out',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes fade-out': {
              to: { opacity: value },
            },
          }),
        },
        { values: { 0: '0', 50: '0.5', 100: '1' } }
      );

      // Add zoom utilities
      matchUtilities(
        {
          'zoom-in': (value) => ({
            animationName: 'zoom-in',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes zoom-in': {
              from: { opacity: '0', transform: `scale(${value})` },
            },
          }),
          'zoom-out': (value) => ({
            animationName: 'zoom-out',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes zoom-out': {
              to: { opacity: '0', transform: `scale(${value})` },
            },
          }),
        },
        { values: { 0: '0', 50: '0.5', 75: '0.75', 90: '0.9', 95: '0.95', 100: '1', 105: '1.05', 110: '1.1', 125: '1.25', 150: '1.5' } }
      );

      // Add slide utilities
      matchUtilities(
        {
          'slide-in-from-top': (value) => ({
            animationName: 'slide-in-from-top',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes slide-in-from-top': {
              from: { transform: `translateY(-${value})` },
            },
          }),
          'slide-in-from-bottom': (value) => ({
            animationName: 'slide-in-from-bottom',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes slide-in-from-bottom': {
              from: { transform: `translateY(${value})` },
            },
          }),
          'slide-in-from-left': (value) => ({
            animationName: 'slide-in-from-left',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes slide-in-from-left': {
              from: { transform: `translateX(-${value})` },
            },
          }),
          'slide-in-from-right': (value) => ({
            animationName: 'slide-in-from-right',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes slide-in-from-right': {
              from: { transform: `translateX(${value})` },
            },
          }),
          'slide-out-to-top': (value) => ({
            animationName: 'slide-out-to-top',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes slide-out-to-top': {
              to: { transform: `translateY(-${value})` },
            },
          }),
          'slide-out-to-bottom': (value) => ({
            animationName: 'slide-out-to-bottom',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes slide-out-to-bottom': {
              to: { transform: `translateY(${value})` },
            },
          }),
          'slide-out-to-left': (value) => ({
            animationName: 'slide-out-to-left',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes slide-out-to-left': {
              to: { transform: `translateX(-${value})` },
            },
          }),
          'slide-out-to-right': (value) => ({
            animationName: 'slide-out-to-right',
            animationDuration: '200ms',
            animationTimingFunction: 'ease-out',
            '@keyframes slide-out-to-right': {
              to: { transform: `translateX(${value})` },
            },
          }),
        },
        { values: { 0: '0', '1/2': '50%', full: '100%', '48%': '48%' } }
      );
    },
  ],
}
