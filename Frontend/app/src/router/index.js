import Vue from 'vue';
import VueRouter from 'vue-router';
import Home from '@/views/Home.vue';
import Books from '@/views/Books.vue';
import Orders from '@/views/Orders.vue';
import Invoices from '@/views/Invoices.vue';
import ManagementPanel from '@/views/ManagementPanel.vue';

Vue.use(VueRouter)

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Home
  },
  {
    path: '/books',
    name: 'Książki',
    component: Books
  },
  {
    path: '/orders',
    name: 'Zamówienia',
    component: Orders
  },
  {
    path: '/invoices',
    name: 'Faktury',
    component: Invoices
  },
  {
    path: '/management',
    name: 'Management',
    component: ManagementPanel
  },
]

const router = new VueRouter({
  routes
})

export default router
