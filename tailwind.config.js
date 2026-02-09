/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./modules/**/*.{html,js}",
    "./index.html",
    "./wwwroot/**/*.html"
  ],
  theme: {
    extend: {
      colors: {
        ocean: {
          50: '#eff9ff',
          100: '#d6f0ff',
          200: '#b5e0ff',
          300: '#86c8ff',
          400: '#55a7f5',
          500: '#2f88db',
          600: '#1c6db8',
          700: '#165697',
          800: '#183f6f',
          900: '#142f55'
        }
      }
    }
  },
  plugins: []
}
