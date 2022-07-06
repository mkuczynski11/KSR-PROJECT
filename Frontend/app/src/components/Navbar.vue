<template>
  <b-button-group>
    <b-button
      v-for="pathElement in PATHS"
      :key="pathElement.path"
      :variant="pathElement.isCurrent ? 'warning' : 'light'"
      @click="changeView(pathElement)"
    >
      {{ pathElement.name }}
    </b-button>
  </b-button-group>
</template>

<script>

const PATHS = [
  {
    path: "/",
    name: "Home",
    isCurrent: false,
  },
  {
    path: "/books",
    name: "Książki",
    isCurrent: false,
  },
  {
    path: "/orders",
    name: "Zamówienia",
    isCurrent: false,
  },
  {
    path: "/invoices",
    name: "Faktury",
    isCurrent: false,
  },
];

export default {
  name: "Navbar",

  data: function () {
    return {
      PATHS,
    };
  },

  created() {
    this.setActiveRoute();
  },

  methods: {
    changeView(pathElement) {
      for (let path of this.PATHS) {
        path.isCurrent = false;
      }
      pathElement.isCurrent = true;
      this.$router.push(pathElement.path);
      this.setActiveRoute();
    },

    setActiveRoute() {
      for (let path of this.PATHS) {
        if (path.path == this.$router.currentRoute.path) {
          path.isCurrent = true;
          break;
        }
      }
    }
  },
};
</script>
